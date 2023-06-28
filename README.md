# Pixels Character Sheet

This project is a C#/Blazor WebAssembly browser based page for use with the Pixels dice.
(see https://github.com/GameWithPixels for more about the project)

A live version of the site is available at https://web.vaettir.net/roller.

Everything is handled/stored client side, the server only provides the static resources.

Because Pixels use Bluetooth, this project will only be able to connect to the Pixels dice using a browser that supports
Bluetooth. At the moment I believe that's generally Chrome and Edge on windows, and Bluefy on iOS.

# How to build the project

The project requires dotnet 7 and npm to build.  On windows, you can run;
```
winget install Microsoft.DotNet.SDK.7 OpenJS.NodeJS.LTS
```

Then build the project with "dotnet build" or in any IDE.