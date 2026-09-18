# Dependencies and licences

## Foundation

| Component | Version / source | Purpose | Licence |
| --- | --- | --- | --- |
| .NET / WPF | .NET 10 shared framework; SDK selected by `global.json` | Runtime and Windows desktop UI | [.NET runtime MIT](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT), [WPF MIT](https://github.com/dotnet/wpf/blob/main/LICENSE.TXT); upstream third-party notices also apply |
| MSTest | 4.4.1 NuGet package, test project only | Test framework, adapter and tooling | [MIT](https://github.com/microsoft/testfx/blob/main/LICENSE), [package metadata](https://www.nuget.org/packages/MSTest/4.4.1) |

The desktop and Core projects currently use only the .NET shared framework and each other. No image codec or SVG optimiser is installed in this milestone. Dependency resolution is recorded in each project's `packages.lock.json`.

### Transitive test tooling

The MSTest metapackage also restores the following development-only dependencies. These entries were checked against the restored packages' `.nuspec` licence metadata; they are not dependencies of the desktop executable.

| Packages | Resolved version | Licence |
| --- | --- | --- |
| MSTest.Analyzers, MSTest.TestAdapter, MSTest.TestFramework | 4.4.1 | MIT |
| Microsoft.NET.Test.Sdk, Microsoft.TestPlatform.ObjectModel, Microsoft.TestPlatform.TestHost, Microsoft.CodeCoverage | 18.9.0 | MIT |
| Microsoft.Testing.Platform, Microsoft.Testing.Platform.MSBuild, Microsoft.Testing.Extensions.Telemetry, Microsoft.Testing.Extensions.TrxReport, Microsoft.Testing.Extensions.TrxReport.Abstractions | 2.4.1 | MIT |
| Microsoft.ApplicationInsights | 2.23.0 | MIT |
| Microsoft.DiaSymReader | 2.2.10 | MIT |
| Microsoft.Extensions.DependencyModel | 10.0.10 | MIT |
| Microsoft.Testing.Extensions.CodeCoverage | 18.11.0 | Microsoft .NET Library terms in the package's `License.txt`; **not MIT**. See the [versioned package and licence link](https://www.nuget.org/packages/Microsoft.Testing.Extensions.CodeCoverage/18.11.0). |

Keep upstream third-party notices when distributing any tooling. Recheck this inventory when updating the lock file.

## Development tools

- VS Code's C# extension source is MIT; packaged components have their accompanying notices.
- C# Dev Kit is optional proprietary Microsoft tooling under its own terms, not an application runtime dependency. See the [official licensing FAQ](https://code.visualstudio.com/docs/csharp/cs-dev-kit-faq#_licensing-and-contributing).
- Visual Studio has its own edition/subscription terms.

## Image-library decision: next milestone

Select the raster library when implementing import so that decoder behaviour, orientation, WebP encoder controls, memory costs and transparency can be checked against real fixtures. Document the exact version, licence, bundled native codecs and distribution notices at that point. Do not assume every image library is unrestricted for commercial distribution.

SVG optimisation must preserve vector data. A raster library's ability to render SVG does not establish that it can safely optimise SVG XML. Choose and test that path separately; external references, scripts, viewBox/units and unsupported SVG features require explicit handling.
