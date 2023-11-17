# COCOAnnotator

This is a tool that creates, edits, manages custom annotation dataset files in [COCO dataset](https://cocodataset.org/#home) format for object detection.

## Features

* You can add, edit, delete object detection annotations to imported images.
* You can export annotations to the file or import existing files.
* You can batch delete images or rename categories in the dataset.
* You can make detailed statistics of the dataset, including the number of images in the dataset.
* You can merge multiple dataset files or split one into according to a given scenario.
* You can find and remove redundant bounding boxes that have high similarity in the dataset.

*NOTE* This tool currently supports only Korean.

## How to build and run

You need a Windows PC with .NET 8.0 SDK installed. Download and install .NET 8.0 SDK from [.NET download website](https://dotnet.microsoft.com/en-us/download), then execute the following command from the top folder of the repository.

```
dotnet publish -c Release -p:PublishProfile=Properties\PublishProfiles\ReleaseBuild.pubxml
```

Alternatively, you can build it with open the solution in Visual Studio 2022 and run `Build - Publish Selection` from menu. Once build finished, `COCOAnnotator.exe`, a standalone exe executable file, will be generated under `bin\Release\net8.0-windows7.0\publish\win-x64` folder. You can run the executable file by moving it to the PC you want to run.

If you only want to run, not build, then you can install smaller .NET 8.0 runtime instead of SDK.
