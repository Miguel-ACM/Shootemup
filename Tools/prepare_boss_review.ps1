$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$review = Join-Path $root 'Build/ReviewCache/Temp/DecisionHoverReview'
# Snapshot only this pass over the committed game. Other chats are authoring items/audio concurrently.
$owned = @(
 'Assets/Scripts/Run/CloudTransition.cs',
 'Assets/Scripts/Projectiles/CombatRound.cs',
 'Assets/Scripts/Enemies/Bosses/DistrictBoss.cs', 'Assets/Scripts/Enemies/Bosses/PhaseBreak.cs',
 'Assets/Scripts/Enemies/Bosses/SeraphJudicatorBoss.cs', 'Assets/Scripts/Enemies/Bosses/VoidSovereignBoss.cs',
 'Assets/Scripts/Enemies/Bosses/VoidSovereignBoss.Cards.cs', 'Assets/Scripts/Enemies/Bosses/VoidSovereignBoss.Interludes.cs',
 'Assets/Scripts/Enemies/EnemyFiringCue.cs', 'Assets/Scripts/Environment/HeavenBackdrop.cs',
 'Assets/Scripts/Visuals/HeavenAngelVisual.cs','Assets/Scripts/Visuals/PSXImpactEffects.cs',
 'Assets/Scripts/Visuals/Meshes/HeavenAngelMeshes.cs','Assets/Scripts/Visuals/Meshes/VoidForms.cs',
 'Assets/Scripts/Visuals/Meshes/SculptedGeometry.cs','Assets/Scripts/Visuals/Meshes/TerraModels.cs',
 'Assets/Scripts/Visuals/Meshes/CombatHullFactory.cs','Assets/Scripts/Waves/WavePreparation.cs',
 'Assets/Scripts/Enemies/Minibosses/DawnGatekeeper.cs',
 'Assets/Scripts/Enemies/Ships/ChoirLancer.cs','Assets/Scripts/Enemies/Ships/AegisReliquary.cs',
 'Assets/Scripts/Enemies/Ships/PrismCantor.cs','Assets/Scripts/Enemies/Ships/VotiveSpear.cs','Assets/Scripts/Enemies/Ships/ThroneBarge.cs',
 'Assets/Resources/HeavenClouds.shader','Assets/Resources/VoidBody.shader','Assets/Resources/SpaceBlackHole.shader',
 'Assets/Resources/TerraModels/Hull81.txt','Assets/Resources/TerraModels/Hull82.txt','Assets/Resources/TerraModels/Hull83.txt','Assets/Resources/TerraModels/Hull84.txt'
)
$new = @(
 'Assets/Editor/Checks/VerifyBossPass.cs','Assets/Editor/Checks/VerifyWaveWatcherInput.cs',
 'Assets/Scripts/Enemies/Bosses/VoidSovereignBoss.Teleport.cs','Assets/Scripts/Visuals/BossMechanismRig.cs',
 'Assets/Scripts/Visuals/Meshes/CutGeometry.cs','Assets/Scripts/Visuals/Meshes/HeavenBastions.cs',
 'Assets/Scripts/Visuals/VoidAnatomyVisual.cs','Assets/Scripts/Visuals/VoidRiftVisual.cs'
)
foreach ($path in (git -C $root diff --name-only -- Assets)) {
 if ($path -in $owned -or $path.EndsWith('.meta')) { continue }
 $target = Join-Path $review $path
 $info = [Diagnostics.ProcessStartInfo]::new('git')
 $info.WorkingDirectory=$root; $info.UseShellExecute=$false; $info.RedirectStandardOutput=$true; $info.CreateNoWindow=$true
 $info.ArgumentList.Add('show');$info.ArgumentList.Add('HEAD:'+$path)
 $process = [Diagnostics.Process]::Start($info)
 $stream = [IO.File]::Create($target)
 try { $process.StandardOutput.BaseStream.CopyTo($stream) } finally { $stream.Dispose() }
 $process.WaitForExit()
 if ($process.ExitCode -ne 0) { throw ('Failed to snapshot '+$path) }
}
foreach ($path in (git -C $root ls-files --others --exclude-standard -- Assets)) {
 if (!$path.EndsWith('.cs') -or $path -in $new) { continue }
 $target = [IO.Path]::GetFullPath((Join-Path $review $path))
 $destination = [IO.Path]::GetFullPath((Join-Path $review ('StaleScripts/BossPassConcurrent/'+$path)))
 if (!$target.StartsWith($review+[IO.Path]::DirectorySeparatorChar) -or !$destination.StartsWith($review+[IO.Path]::DirectorySeparatorChar)) { throw 'Unexpected review path' }
 if (Test-Path -LiteralPath $target) {
  [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
  Move-Item -LiteralPath $target -Destination $destination -Force
  if (Test-Path -LiteralPath ($target+'.meta')) { Move-Item -LiteralPath ($target+'.meta') -Destination ($destination+'.meta') -Force }
 }
}
