# SharpILMixins

SharpILMixins is a trait/mixin and IL weaving framework for C# using [dnLib](https://github.com/0xd4d/dnlib).


## Creating a Mixin project with SharpILMixins.

SharpILMixins provides a project template with the NuGet Package Id `SharpILMixin.Template`.

To install the template with `dotnet new`, run the following command on the Terminal of your choice:
```
dotnet new -i SharpILMixin.Template
```

Then, you can create a new project with this template by running the following command:
```
dotnet new sharpilmixins [-na <namespace>] [-t <target name>] [-o <output folder>]
```

More information:
```
Mixin with SharpILMixins
Author: NickAc
Options:
  -na|--namespace
                   string - Optional
                   Default: SharpILMixins.Template

  -t|--target
                   string - Optional
                   Default: MixinTarget.dll
```

## Runtime detour

This project does not support runtime detouring. The aim is to allow the user to modify their .NET Assemblies and do an offline replacement of the original files.


