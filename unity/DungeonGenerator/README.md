# Unity Dungeon Room Generator

Unity에서 사용할 수 있는 던전 방 생성기입니다. Shattered Pixel Dungeon의 레벨 빌더 시스템을 기반으로 구현되었습니다.

## 개요

이 패키지는 절차적 던전 생성을 위한 다양한 빌더 패턴을 제공합니다:

- **LoopBuilder**: 원형/타원형 루프 형태의 던전
- **LineBuilder**: 직선형 던전
- **BranchesBuilder**: 가지치기 형태의 던전  
- **FigureEightBuilder**: 8자 형태 (두 개의 연결된 루프) 던전

## 설치 방법

1. `DungeonGenerator` 폴더를 Unity 프로젝트의 `Assets/Scripts` 폴더에 복사합니다.
2. 원하는 GameObject에 `DungeonGenerator` 컴포넌트를 추가합니다.

## 기본 사용법

### Inspector를 통한 사용

```
1. 빈 GameObject를 생성합니다.
2. DungeonGenerator 컴포넌트를 추가합니다.
3. Inspector에서 원하는 설정을 조정합니다.
4. Play 모드에서 GenerateDungeon() 메서드를 호출합니다.
```

### 스크립트를 통한 사용

```csharp
using DungeonGenerator;
using System.Collections.Generic;
using UnityEngine;

public class MyDungeonController : MonoBehaviour
{
    private DungeonGenerator.DungeonGenerator generator;

    void Start()
    {
        generator = gameObject.AddComponent<DungeonGenerator.DungeonGenerator>();
        
        // 설정
        generator.layoutType = DungeonLayoutType.Loop;
        generator.standardRoomCount = 12;
        generator.includeShop = true;
        
        // 던전 생성
        List<Room> rooms = generator.GenerateDungeon();
        
        // 생성된 방들 처리
        foreach (Room room in rooms)
        {
            CreateRoomGameObject(room);
        }
    }
    
    void CreateRoomGameObject(Room room)
    {
        // 방의 위치와 크기로 실제 게임 오브젝트 생성
        Vector3 position = new Vector3(
            (room.left + room.right) / 2f,
            0,
            (room.top + room.bottom) / 2f
        );
        
        Vector3 size = new Vector3(room.Width(), 1, room.Height());
        
        // 실제 방 오브젝트 생성 로직...
    }
}
```

## 빌더 직접 사용하기

컴포넌트 대신 빌더 클래스를 직접 사용할 수도 있습니다:

```csharp
using DungeonGenerator;
using System.Collections.Generic;

// 방 목록 생성
List<Room> rooms = new List<Room>();
rooms.Add(new Room(RoomType.Entrance));
rooms.Add(new Room(RoomType.Exit));

for (int i = 0; i < 10; i++)
{
    rooms.Add(new Room(RoomType.Standard));
}

// LoopBuilder 사용
LoopBuilder builder = new LoopBuilder();
builder.SetPathVariance(45f);
builder.SetPathLength(0.5f, new float[] { 0, 1, 0 });
builder.SetLoopShape(0, 1f, 0f); // 원형 루프

List<Room> generatedRooms = builder.Build(rooms);

// 생성 실패 시 null 반환
if (generatedRooms == null)
{
    Debug.LogError("던전 생성 실패!");
}
```

## 클래스 구조

### Room (방)

```csharp
Room room = new Room(RoomType.Standard);

// 속성
room.left;      // 왼쪽 좌표
room.right;     // 오른쪽 좌표
room.top;       // 위 좌표
room.bottom;    // 아래 좌표
room.Width();   // 너비
room.Height();  // 높이

// 연결 정보
room.neighbours;  // 인접한 방들
room.connected;   // 연결된 방들 (Door 정보 포함)

// 유틸리티
room.Center();    // 중심점
room.Random();    // 방 내부의 랜덤 포인트
room.Inside(point); // 포인트가 방 내부인지 확인
```

### RoomType (방 유형)

```csharp
public enum RoomType
{
    Standard,    // 일반 방
    Entrance,    // 입구
    Exit,        // 출구
    Connection,  // 연결 통로
    Secret,      // 비밀 방
    Shop,        // 상점
    Special      // 특수 방
}
```

### RoomSizeCategory (방 크기)

```csharp
public enum RoomSizeCategory
{
    Normal,  // 일반 크기
    Large,   // 큰 방
    Giant    // 거대한 방
}
```

## 빌더 설정

### 공통 설정

```csharp
builder.SetPathVariance(45f);           // 경로 방향 변동폭 (도)
builder.SetPathLength(0.5f, jitter);    // 메인 경로에 포함될 방 비율
builder.SetTunnelLength(path, branch);  // 터널 길이 확률
builder.SetExtraConnectionChance(0.2f); // 추가 연결 확률
```

### LoopBuilder 설정

```csharp
LoopBuilder loop = new LoopBuilder();
loop.SetLoopShape(
    exponent: 0,      // 곡률 (0 = 원, 높을수록 타원형)
    intensity: 1f,    // 곡률 강도 (0-1)
    offset: 0f        // 시작점 오프셋
);
```

### FigureEightBuilder 설정

```csharp
FigureEightBuilder figure8 = new FigureEightBuilder();
figure8.SetLoopShape(0, 1f, 0f);
figure8.SetLandmarkRoom(landmarkRoom);  // 교차점이 될 방 지정
```

## 랜덤 시드 설정

재현 가능한 결과를 위해 랜덤 시드를 설정할 수 있습니다:

```csharp
DungeonRandom.SetSeed(12345);
```

## 시각화

DungeonGenerator 컴포넌트는 Scene 뷰에서 Gizmos를 통해 던전을 시각화합니다:

- 초록색: 입구
- 빨간색: 출구
- 흰색: 일반 방
- 회색: 연결 통로
- 노란색: 상점
- 보라색: 비밀 방
- 파란 선: 방 간의 연결

## 고급 사용법

### 커스텀 방 유형 만들기

```csharp
public class BossRoom : Room
{
    public BossRoom() : base(RoomType.Special)
    {
        // 보스 방은 더 큰 크기
        _minWidth = 10;
        _maxWidth = 15;
        _minHeight = 10;
        _maxHeight = 15;
    }
    
    public override int MaxConnections(int direction)
    {
        // 연결은 하나만 허용
        return direction == ALL ? 1 : 1;
    }
}
```

### 타일맵으로 렌더링

```csharp
using UnityEngine.Tilemaps;

public void RenderToTilemap(Tilemap tilemap, List<Room> rooms)
{
    foreach (Room room in rooms)
    {
        // 바닥 타일 배치
        for (int x = room.left; x <= room.right; x++)
        {
            for (int y = room.top; y <= room.bottom; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                
                // 가장자리는 벽, 내부는 바닥
                if (x == room.left || x == room.right || 
                    y == room.top || y == room.bottom)
                {
                    tilemap.SetTile(pos, wallTile);
                }
                else
                {
                    tilemap.SetTile(pos, floorTile);
                }
            }
        }
        
        // 문 배치
        foreach (var connection in room.connected)
        {
            Door door = connection.Value;
            if (door != null)
            {
                Vector3Int doorPos = new Vector3Int(
                    door.position.x, door.position.y, 0);
                tilemap.SetTile(doorPos, doorTile);
            }
        }
    }
}
```

## 라이선스

이 코드는 GNU General Public License v3.0 라이선스를 따릅니다.
원본 Shattered Pixel Dungeon: Copyright (C) 2014-2021 Evan Debenham

## 참고

- [Shattered Pixel Dungeon GitHub](https://github.com/00-Evan/shattered-pixel-dungeon)
- 원본 레벨 빌더 경로: `core/src/main/java/com/shatteredpixel/shatteredpixeldungeon/levels/builders/`
