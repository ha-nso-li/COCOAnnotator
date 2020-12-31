# COCOAnnotator

객체 검출을 위한 [COCO 데이터셋](https://cocodataset.org/) 포맷의 커스텀 어노테이션 데이터셋 파일을 생성하고 편집, 관리하는 도구입니다.

## Features

* 이미지를 불러와 객체 검출 어노테이션을 추가, 편집, 삭제할 수 있습니다.
* 어노테이션을 파일로 내보내거나 기존 파일을 불러올 수 있습니다.
* 데이터셋에 포함된 이미지 또는 분류를 일괄 삭제 또는 이름 변경할 수 있습니다.
* 데이터셋에 포함된 이미지 갯수를 포함한 상세 통계를 계산할 수 있습니다.
* 여러 데이터셋 파일을 하나로 병합하거나 주어진 시나리오에 맞게 분할할 수 있습니다.
* 데이터셋에서 유사도가 높은 중복 경계 상자를 찾고 제거할 수 있습니다.

## How to build and run

.NET 5.0 SDK가 설치된 Windows PC가 필요합니다. [닷넷 다운로드 웹사이트](https://dotnet.microsoft.com/download)에서 .NET 5.0 SDK를 다운로드 받아 설치하고, 저장소의 최상위 폴더에서 다음 명령어를 실행하세요.

```
dotnet publish -c Release -p:PublishProfile=Properties\PublishProfiles\ReleaseBuild.pubxml
```

또는 Visual Studio 2019에서 솔루션을 열어 Build - Publish COCOAnnotator 메뉴를 통해서도 빌드할 수 있습니다. 빌드가 완료되면 bin/Release/net5.0-windows/publish 폴더 아래에 스탠드얼론 exe 실행 파일인 COCOAnnotator.exe가 생성됩니다. 실행하고자 하는 PC로 실행 파일을 옮겨 실행할 수 있습니다.

빌드 없이 실행만 하면 되는 경우 SDK 대신 용량이 작은 .NET 5.0 Runtime만 설치해도 됩니다. SDK에는 Runtime이 포함되어 있습니다.
