## Token PFM Sample: C#

Simple personal finance app that illustrates Token.io's Access Tokens

This sample app shows how to request Token's Access Tokens, useful
for fetching account information.

## Requirements

### On Windows

There are no prerequisites for Windows.

### On Linux and OSX

Install `Mono` from [here](https://www.mono-project.com/download/stable/).

 `Mono` is an open source implementation of Microsoft's .NET Framework. It brings the .NET framework to non-Windows envrionments like Linux and OSX.

## Build and Run

To build

``` 
nuget restore

msbuild
```

To run 
```
xsp4 --address=localhost --port=3000
```

This starts up a server.

The server operates against Token's Sandbox environment by default.
This testing environment lets you try out UI and account flows without
exposing real bank accounts.

The server shows a web page at `localhost:3000`. The page has a Link with Token button.
Clicking the button displays Token UI that requests an Access Token.
When the app has an Access Token, it uses that Access Token to get account balances.

### Troubleshooting

If anything goes wrong, try to clear your browser's cache before retest.

If you see the following error: "The CodeDom provider type “Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider” could not be located",
try removing the NuGet packages Microsoft.CodeDom.Providers.DotNetCompilerPlatform and Microsoft.Net.Compilers
as per the advice in https://stackoverflow.com/questions/33319675/the-codedom-provider-type-microsoft-codedom-providers-dotnetcompilerplatform-cs.
