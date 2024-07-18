## PlaywrightUtilities

Convenience methods for using Playwright in F# (i.e. TryRepeatedly, tryStep)

## Install

First add the package source
```pwsh
dotnet nuget add source "https://nuget.pkg.github.com/fischorg/index.json" --name fischorg --username "PUBLIC_GITHUB_PACKAGES_USER%" --password "%PUBLIC_GITHUB_PACKAGES_PASSWORD%" --store-password-in-clear-text
```

To use from Paket add this to your paket.dependencies file
```
source https://nuget.pkg.github.com/fischorg/index.json username: "%PUBLIC_GITHUB_PACKAGES_USER%" password: "%PUBLIC_GITHUB_PACKAGES_PASSWORD%"
```