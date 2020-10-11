# LabelAnnotator

객체 검출에 사용될 수 있는 CSV 스타일의 어노테이션 라벨링 파일을 생성하고 편집, 관리하는 도구입니다.

## Features

 * 이미지를 불러와 객체 검출 어노테이션을 추가, 편집, 삭제할 수 있습니다.
 * 어노테이션을 라벨링 파일로 내보내거나 기존 라벨링 파일을 불러올 수 있습니다. 라벨링 파일은 CSV 형식이며 포맷은 다음과 같습니다.
    * `(파일 이름),(x1),(y1),(x2),(y2),(분류 이름)`
    * 경계 상자 좌표 형식은 `L,T,R,B` (기본값, 정수), `CX,CY,W,H` (실수)를 지원합니다.
 * 라벨링 파일에 포함된 이미지 또는 분류를 일괄 삭제 또는 이름 변경할 수 있습니다.
 * 라벨링 파일에 포함된 이미지 갯수를 포함한 상세 통계를 계산할 수 있습니다.
 * 여러 라벨링 파일을 하나로 병합하거나 주어진 시나리오에 맞게 분할할 수 있습니다.
 * 라벨링 파일에서 유사도가 높은 중복 경계 상자를 찾고 제거할 수 있습니다.

## Requirements

.NET Core 3.1에서 동작합니다. [닷넷 다운로드 웹사이트](https://dotnet.microsoft.com/download)에서 .NET Core 3.1 데스크탑 앱 런타임 또는 SDK를 다운로드 받아 설치하세요.

## How to Build

저장소의 최상위 폴더에서 다음 명령어를 실행하세요.

```
dotnet publish -c Release -p:PublishProfile=Properties\PublishProfiles\ReleaseBuild.pubxml
```

또는 Visual Studio 2019에서 솔루션을 열어 Build - Publish LabelAnnotator 메뉴를 통해서도 게시할 수 있습니다.
