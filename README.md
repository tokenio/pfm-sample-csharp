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

## Build

To build

``` 
nuget restore

msbuild
```

To run 
```
mono  /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/xsp4.exe --address=127.0.0.1 --port=3000
```

`Note: the path to xsp4.exe might be different in Linux and Windows`

This starts up a server.

The server operates against Token's Sandbox environment by default.
This testing environment lets you try out UI and account flows without
exposing real bank accounts.

The server shows a web page at `127.0.0.1:3000`. The page has a Link with Token button.
Clicking the button displays Token UI that requests an Access Token.
When the app has an Access Token, it uses that Access Token to get account balances.
