# Publishing and store submission

## GitHub releases

Pushes to `main` and pull requests run the Windows build, regression checks, and ZIP packaging. Download `plugin-package` from the Actions run to test a build.

For a release, update the versions in `plugin.json` and the plugin csproj, commit the changes, and push the matching version tag:

```powershell
git push origin main
git tag v1.0.1
git push origin v1.0.1
```

When the tag matches the manifest version and verification passes, Actions creates a GitHub Release with the installation ZIP. It uses the workflow token; no personal token is needed. Automated checks never execute real input or power operations.

## Store submission

1. Verify that the release ZIP downloads successfully.
2. Update the version and URLs in `docs/store/DoAction-D5B032E0FE594208B0687010889D6450.json`.
3. Fork https://github.com/Flow-Launcher/Flow.Launcher.PluginsManifest.
4. Copy the submission JSON into the fork's `plugins/` directory and submit a PR to the official default branch.

`MinimumAppVersion` is 2.1.4, the tested host version. Publishing a release and obtaining store approval are separate steps. Keep published tags and release assets intact; publish changes as a new version.

[Official submission instructions](https://github.com/Flow-Launcher/Flow.Launcher.PluginsManifest#how-to-submit-your-plugin)
