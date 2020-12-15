# LabelAnnotator

객체 검출에 사용될 수 있는 CSV 스타일의 어노테이션 라벨링 파일을 생성하고 편집, 관리하는 도구입니다.

## Features

* 이미지를 불러와 객체 검출 어노테이션을 추가, 편집, 삭제할 수 있습니다.
* 어노테이션을 라벨링 파일로 내보내거나 기존 라벨링 파일을 불러올 수 있습니다. 라벨링 파일은 CSV 형식이며 포맷은 다음과 같습니다.
  * `(파일 이름),(x1),(y1),(x2),(y2),(분류 이름)`
  * 경계 상자 좌표 형식은 `L,T,R,B` (기본값), `L,T,W,H`, `CX,CY,W,H`를 지원합니다.
* 라벨링 파일에 포함된 이미지 또는 분류를 일괄 삭제 또는 이름 변경할 수 있습니다.
* 라벨링 파일에 포함된 이미지 갯수를 포함한 상세 통계를 계산할 수 있습니다.
* 여러 라벨링 파일을 하나로 병합하거나 주어진 시나리오에 맞게 분할할 수 있습니다.
* 라벨링 파일에서 유사도가 높은 중복 경계 상자를 찾고 제거할 수 있습니다.

## How to build and run

.NET Core 3.1 SDK가 설치된 Windows PC가 필요합니다. [닷넷 다운로드 웹사이트](https://dotnet.microsoft.com/download)에서 .NET Core 3.1 SDK를 다운로드 받아 설치하고, 저장소의 최상위 폴더에서 다음 명령어를 실행하세요.

```
dotnet publish -c Release -p:PublishProfile=Properties\PublishProfiles\ReleaseBuild.pubxml
```

또는 Visual Studio 2019에서 솔루션을 열어 Build - Publish LabelAnnotator 메뉴를 통해서도 빌드할 수 있습니다. 빌드가 완료되면 bin/Release/netcoreapp3.1/publish 폴더 아래에 스탠드얼론 exe 실행 파일인 LabelAnnotator.exe가 생성됩니다. 실행하고자 하는 PC로 실행 파일을 옮겨 실행할 수 있습니다.

빌드 없이 실행만 하면 되는 경우 SDK 대신 용량이 작은 .NET Core 3.1 Desktop Runtime만 설치해도 됩니다. SDK에는 Runtime이 포함되어 있습니다.
