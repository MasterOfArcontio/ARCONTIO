[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]] $Branches,

    [switch] $DeleteRemote,

    [ValidateNotNullOrEmpty()]
    [string] $Remote = "origin",

    [ValidateNotNullOrEmpty()]
    [string] $BaseBranch = "ai/codex-main"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-GitCommand {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($null -ne $git) {
        return $git.Source
    }

    $fallback = "C:\Program Files\Git\cmd\git.exe"
    if (Test-Path -LiteralPath $fallback) {
        return $fallback
    }

    throw "Git executable not found. Install Git or add it to PATH."
}

$GitCommand = Resolve-GitCommand

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    & $GitCommand @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Read-GitOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    $output = & $GitCommand @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }

    return $output
}

function Test-GitRefExists {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Ref
    )

    & $GitCommand show-ref --verify --quiet $Ref
    return $LASTEXITCODE -eq 0
}

function Assert-CleanWorktree {
    $porcelain = @(Read-GitOutput @("status", "--porcelain"))
    if ($porcelain.Length -gt 0) {
        Write-Host "Worktree is dirty. Consolidation stopped before checkout/pull." -ForegroundColor Red
        Write-Host ""
        $porcelain | ForEach-Object { Write-Host $_ }
        exit 1
    }
}

function Assert-NoOperationInProgress {
    $mergeHead = Read-GitOutput @("rev-parse", "--git-path", "MERGE_HEAD")
    $rebaseMerge = Read-GitOutput @("rev-parse", "--git-path", "rebase-merge")
    $rebaseApply = Read-GitOutput @("rev-parse", "--git-path", "rebase-apply")

    if ((Test-Path -LiteralPath $mergeHead) -or
        (Test-Path -LiteralPath $rebaseMerge) -or
        (Test-Path -LiteralPath $rebaseApply)) {
        Write-Host "Merge or rebase operation is in progress. Consolidation stopped." -ForegroundColor Red
        exit 1
    }
}

function Test-LocalBranchMerged {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Branch,

        [Parameter(Mandatory = $true)]
        [string] $IntoBranch
    )

    & $GitCommand merge-base --is-ancestor $Branch $IntoBranch
    return $LASTEXITCODE -eq 0
}

function Test-RemoteBranchMerged {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RemoteBranch,

        [Parameter(Mandatory = $true)]
        [string] $IntoRef
    )

    & $GitCommand merge-base --is-ancestor $RemoteBranch $IntoRef
    return $LASTEXITCODE -eq 0
}

if ($BaseBranch -eq "main") {
    throw "Refusing to consolidate on main. Use ai/codex-main as the base branch."
}

if (@($Branches).Length -eq 0) {
    throw "At least one branch name must be provided with -Branches."
}

$repoRoot = Read-GitOutput @("rev-parse", "--show-toplevel")
Set-Location -LiteralPath $repoRoot

Write-Host "Repository: $repoRoot"
Write-Host "Base branch: $BaseBranch"
Write-Host "Remote: $Remote"
Write-Host ""

Assert-CleanWorktree
Assert-NoOperationInProgress

Write-Host "Fetching $Remote..."
Invoke-Git @("fetch", $Remote)

$baseRef = "refs/heads/$BaseBranch"
if (-not (Test-GitRefExists $baseRef)) {
    Write-Host "Local base branch '$BaseBranch' does not exist. Creating it from '$Remote/$BaseBranch'."
    Invoke-Git @("checkout", "-b", $BaseBranch, "$Remote/$BaseBranch")
} else {
    Write-Host "Checking out $BaseBranch..."
    Invoke-Git @("checkout", $BaseBranch)
}

if ($BaseBranch -eq "main") {
    throw "Refusing to continue on main."
}

Write-Host "Pulling latest $Remote/$BaseBranch..."
Invoke-Git @("pull", $Remote, $BaseBranch)

foreach ($branch in $Branches) {
    if ([string]::IsNullOrWhiteSpace($branch)) {
        Write-Host "Skipping blank branch name." -ForegroundColor Yellow
        continue
    }

    if ($branch -eq "main" -or $branch -eq $BaseBranch) {
        Write-Host "Skipping protected branch '$branch'." -ForegroundColor Yellow
        continue
    }

    Write-Host ""
    Write-Host "Inspecting branch: $branch"

    $localRef = "refs/heads/$branch"
    $localExists = Test-GitRefExists $localRef

    if ($localExists) {
        if (Test-LocalBranchMerged $branch $BaseBranch) {
            Write-Host "Local branch is merged into $BaseBranch. Deleting local branch."
            Invoke-Git @("branch", "-d", $branch)
        } else {
            Write-Host "WARNING: local branch '$branch' is not merged into $BaseBranch. Not deleting." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Local branch '$branch' does not exist. Nothing to delete locally."
    }

    if ($DeleteRemote) {
        $remoteRef = "refs/remotes/$Remote/$branch"
        $remoteExists = Test-GitRefExists $remoteRef

        if ($remoteExists) {
            $remoteBranch = "$Remote/$branch"
            $remoteBase = "$Remote/$BaseBranch"

            if (Test-RemoteBranchMerged $remoteBranch $remoteBase) {
                Write-Host "Remote branch is merged into $remoteBase. Deleting remote branch."
                Invoke-Git @("push", $Remote, "--delete", $branch)
            } else {
                Write-Host "WARNING: remote branch '$remoteBranch' is not merged into $remoteBase. Not deleting." -ForegroundColor Yellow
            }
        } else {
            Write-Host "Remote branch '$Remote/$branch' does not exist. Nothing to delete remotely."
        }
    }
}

Write-Host ""
Write-Host "Final status:"
Invoke-Git @("status", "--short", "--branch")

Write-Host ""
Write-Host "Last 5 commits:"
Invoke-Git @("log", "--oneline", "-5")
