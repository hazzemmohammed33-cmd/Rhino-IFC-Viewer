# Dependency inventory — Ticket 01

This inventory describes the actual native restore and build for Ticket 01, using `src/RhinoIfcViewer/packages.lock.json`. It is not the final web/test/distribution audit. Package payloads are unmodified. SDK: 10.0.400; target: `net8.0-windows`, x64. The SDK version does not change the runtime target.

## Resolved packages and disposition

Each package has a version-named directory under `licenses/` containing its original `.nuspec`, plus available package notices/licenses. Downloaded upstream license text has a `SOURCE.txt` with its exact origin. The lockfile retains package content hashes.

| Package | Version | Kind | Declared terms / retained evidence | Native output action |
|---|---|---|---|---|
| Microsoft.Web.WebView2 | 1.0.4191.47 | Direct | Microsoft SDK license; package LICENSE.txt and NOTICE.txt | Include Core/WinForms wrappers and native loaders; omit unused WPF wrapper |
| RhinoCommon | 8.29.26063.11001 | Direct | McNeel SDK/host; no license field in package metadata; accompanying Eto license retained | Compile-only; no RhinoCommon or Eto DLL in output |
| Xbim.Essentials | 6.1.605 | Direct | CDDL family source license; legacy package license URL retained in metadata | Metapackage; no Essentials DLL |
| Microsoft.Database.ManagedEsent | 2.0.3 | Transitive | MIT; upstream license and original copyright metadata | Include Esent.Interop.dll |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | Transitive | MIT; package license and third-party notices | Include assembly |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.9 | Transitive | MIT; upstream license and package third-party notices | Include assembly |
| Microsoft.Extensions.Logging.Abstractions | 8.0.3 | Transitive | MIT; package license and third-party notices | Include assembly |
| Microsoft.Extensions.Options | 10.0.9 | Transitive | MIT; upstream license and package third-party notices | Include assembly |
| Microsoft.Extensions.Primitives | 10.0.9 | Transitive | MIT; upstream license and package third-party notices | Include assembly |
| Microsoft.Win32.SystemEvents | 7.0.0 | Transitive via RhinoCommon | MIT; package license and third-party notices | Not copied; host/framework dependency |
| System.Drawing.Common | 7.0.0 | Transitive via RhinoCommon | MIT; package license and third-party notices | Not copied; host/framework dependency |
| Xbim.Common | 6.1.605 | Transitive | CDDL-1.0; upstream license and copyright metadata | Include assembly |
| Xbim.Ifc | 6.1.605 | Transitive | CDDL-1.0; upstream license and copyright metadata | Include assembly |
| Xbim.Ifc2x3 | 6.1.605 | Transitive | CDDL-1.0; upstream license and copyright metadata | Include assembly |
| Xbim.Ifc4 | 6.1.605 | Transitive | CDDL-1.0; upstream license and copyright metadata | Include assembly |
| Xbim.Ifc4x3 | 6.1.605 | Transitive | CDDL-1.0; upstream license and copyright metadata | Include assembly |
| Xbim.IO.Esent | 6.1.605 | Transitive | CDDL-1.0; upstream license and copyright metadata | Include assembly |
| Xbim.IO.MemoryModel | 6.1.605 | Transitive | CDDL-1.0; upstream license and copyright metadata | Include assembly |

The package graph contains Microsoft.Extensions 10.0.9 dependencies selected by NuGet. Their compatible assets compile for this .NET 8 project. They are not a retarget to .NET 10. Ticket 01 plugin and WinForms/WebView2 control loading subsequently passed in Rhino/.NET 8.0.25. Actual xBIM execution and initialized browser rendering remain NOT RUN until their tickets; a shell load does not exercise every dependency.

## Source provenance and availability

- **xBIM:** [XbimEssentials source at e3c8777](https://github.com/xBimTeam/XbimEssentials/tree/e3c877712aeff39813219b9dd13383c266d07055); [source archive](https://github.com/xBimTeam/XbimEssentials/archive/e3c877712aeff39813219b9dd13383c266d07055.zip). That full commit is recorded by the seven restored xBIM component packages. The metapackage does not itself record a commit; its license copy uses the same component-family source revision. All eight version directories retain CDDL text. No xBIM source was modified.
- **ManagedEsent:** [upstream project](https://github.com/microsoft/ManagedEsent). Its 2.0.3 package declares MIT and Microsoft copyright, but does not record a source commit. The retained license comes from [license-file revision f256bdc](https://github.com/microsoft/ManagedEsent/blob/f256bdc1c8e0e59bdb668eb144190cd2a3cb2a24/LICENSE.md); that is license provenance, not an assertion that this revision built the binary. No v2.0.3 tag was found in the retrieved upstream tag list.
- **Microsoft.Extensions 10.0.9:** [dotnet source at 901ca94](https://github.com/dotnet/dotnet/tree/901ca941248413c79832d2fdbd709da0c4386353), as recorded by those packages; license copied from that commit. Other .NET package metadata retains its individual `dotnet/runtime` commit and original packaged notices.
- **WebView2:** original SDK terms/notices are retained, including its redistribution terms. [SDK information](https://aka.ms/webview). The WebView2 Runtime is a separately installed prerequisite; it is not redistributed here.
- **RhinoCommon:** [McNeel developer documentation](https://developer.rhino3d.com/guides/rhinocommon/). Rhino and Windows are separately licensed prerequisites. The Eto notice in this compile-only package is retained for provenance; no Eto binary is bundled.

## Build correction and loader layout

The pinned WebView2 package's `build/Common.targets` imports both WinForms and WPF wrappers. Its unused WPF wrapper caused MSB3277 (`WindowsBase` 4.0.0.0 versus 5.0.0.0). The project removes only that reference before assembly resolution. Package pins, native loader staging, and the approved WinForms architecture remain unchanged. Warnings were not suppressed.

Verified root loader source:

```text
%USERPROFILE%\.nuget\packages\microsoft.web.webview2\1.0.4191.47\runtimes\win-x64\native\WebView2Loader.dll
```

Verified destination:

```text
src/RhinoIfcViewer/bin/Release/net8.0-windows/WebView2Loader.dll
```

The build also emits `runtimes/win-arm64/native/`, `runtimes/win-x64/native/`, and `runtimes/win-x86/native/` loader subdirectories. Preserve SDK-emitted layout when copying the build. The plugin itself and root loader are x64. No competing RhinoCommon or unused WPF wrapper is copied.

## Audit evidence

The audit ran after restore produced the lockfile. See [resolved package list](evidence/ticket01/resolved-packages.txt), [vulnerability query](evidence/ticket01/vulnerability-audit.txt), [native output check](evidence/ticket01/native-output.txt), and [output hashes](evidence/ticket01/build-output.sha256). The NuGet query reported no known vulnerable packages from its configured sources; that result is not a universal security certification.

The repeatable native checks are `.scratch/ticket01/verify-build.ps1`; license collection/provenance is `.scratch/ticket01/audit-native.ps1`. They are local verification helpers, not shipped plugin code. Web assets, web-ifc, That Open, test-package restore and the final distribution audit belong to later tickets and have not been performed.
