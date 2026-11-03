// World.cs - levels, tiles, containers, doors, hiding spots, traps, nav
using System;
using System.Collections.Generic;

namespace Granny13
{
    public static class T
    {
        public const byte FLOOR = 0, WP = 1, PANEL = 2, BRICK = 3, DOOR = 4, FRONT = 5,
            WARDROBE = 6, CABINET = 7, SHELF = 8, FRIDGE = 9, STOVE = 10, CRATE = 11,
            WINDOW = 12, ATTIC = 13, ATTICWOOD = 14, PORTRAIT = 15, TILEW = 16, TOWELS = 17,
            CAGE = 18, ATTICUP = 19, TABLEW = 20, RUG = 21, HOLE = 22, MIRROR = 23, BOOKS = 24;

        public static readonly bool[] Solid = new bool[32];
        public static readonly int[] WallTex = new int[32];
        public static readonly string[] Name = new string[32];

        static T()
        {
            for (int i = 0; i < 32; i++) { Solid[i] = true; WallTex[i] = TexLib.TW_BRICK; Name[i] = "стена"; }
            Solid[FLOOR] = false; Solid[RUG] = false; Solid[HOLE] = false;
            WallTex[FLOOR] = 0; WallTex[RUG] = 0; WallTex[HOLE] = 0;
            WallTex[WP] = TexLib.TW_WP; WallTex[PANEL] = TexLib.TW_PANEL; WallTex[BRICK] = TexLib.TW_BRICK;
            WallTex[DOOR] = TexLib.TW_DOOR; WallTex[FRONT] = TexLib.TW_FRONT;
            WallTex[WARDROBE] = TexLib.TW_WARDROBE; WallTex[CABINET] = TexLib.TW_CABINET;
            WallTex[SHELF] = TexLib.TW_SHELF; WallTex[FRIDGE] = TexLib.TW_FRIDGE;
            WallTex[STOVE] = TexLib.TW_STOVE; WallTex[CRATE] = TexLib.TW_CRATE;
            WallTex[WINDOW] = TexLib.TW_WINDOW; WallTex[ATTIC] = TexLib.TW_ATTIC;
            WallTex[ATTICWOOD] = TexLib.TW_ATTICWOOD; WallTex[PORTRAIT] = TexLib.TW_PORTRAIT;
            WallTex[TILEW] = TexLib.TW_TILE; WallTex[TOWELS] = TexLib.TW_TOWELS;
            WallTex[CAGE] = TexLib.TW_ATTCAGE; WallTex[ATTICUP] = TexLib.TW_ATTICUP;
            WallTex[TABLEW] = TexLib.TW_TABLE; WallTex[MIRROR] = TexLib.TW_WINDOW;
            WallTex[BOOKS] = TexLib.TW_SHELF;
            Name[FLOOR] = "пол"; Name[RUG] = "ковёр"; Name[HOLE] = "люк";
            Name[DOOR] = "дверь"; Name[FRONT] = "входная дверь"; Name[WARDROBE] = "шкаф";
            Name[CABINET] = "комод"; Name[SHELF] = "стеллаж"; Name[FRIDGE] = "холодильник";
            Name[STOVE] = "плита"; Name[CRATE] = "ящик"; Name[WINDOW] = "окно";
            Name[ATTIC] = "стена"; Name[ATTICWOOD] = "балка"; Name[CAGE] = "клетка";
            Name[TABLEW] = "стол";
        }
    }

    public class Container
    {
        public int X, Y;
        public byte Tile;
        public string Label;
        public int ItemId;      // 0 = empty
        public bool Searched;
        public bool Secret;     // requires prior containers?
        public Container(int x, int y, byte tile, string label)
        {
            X = x; Y = y; Tile = tile; Label = label;
        }
    }

    public class Door
    {
        public int X, Y;
        public bool Open;
        public bool Front;
        public double OpenT;     // animation 0..1
        public Door(int x, int y, bool front) { X = x; Y = y; Front = front; }
    }

    public class HideSpot
    {
        public int X, Y;          // furniture tile
        public int EnterX, EnterY;
        public bool IsBed;
        public string Label;
        public HideSpot(int x, int y, int ex, int ey, bool bed, string label)
        {
            X = x; Y = y; EnterX = ex; EnterY = ey; IsBed = bed; Label = label;
        }
    }

    public class Trap
    {
        public double X, Y;
        public bool Armed = true;
        public bool Visible = true;
        public double TriggeredT;
        public Trap(double x, double y) { X = x; Y = y; }
    }

    public class Deco
    {
        public double X, Y;
        public Sprite Img;
        public double Height;    // world units
        public double RadiusX, RadiusY;
        public bool Solid;
        public int Kind;
        public Deco(double x, double y, Sprite img, double h, double solid)
        {
            X = x; Y = y; Img = img; Height = h; Solid = solid > 0.5;
        }
    }

    public class Noise
    {
        public double X, Y, Radius, Time;
        public string What;
        public Noise(double x, double y, double radius, string what, double time)
        { X = x; Y = y; Radius = radius; What = what; Time = time; }
    }

    public static class Items
    {
        public const int NONE = 0, KEY = 1, CROWBAR = 2, NOTE = 3, MILK = 4, BATTERY = 5, BANDAGE = 6;
        public static string Name(int id)
        {
            switch (id)
            {
                case KEY: return "Ключ от замка";
                case CROWBAR: return "Монтировка";
                case NOTE: return "Записка с кодом";
                case MILK: return "Молоко для Маруси";
                case BATTERY: return "Батарейка";
                case BANDAGE: return "Бинт";
            }
            return "";
        }
        public static string Short(int id)
        {
            switch (id)
            {
                case KEY: return "Ключ";
                case CROWBAR: return "Монтировка";
                case NOTE: return "Код 137";
                case MILK: return "Молоко";
                case BATTERY: return "Батарейка";
                case BANDAGE: return "Бинт";
            }
            return "";
        }
        public static string Hint(int id)
        {
            switch (id)
            {
                case KEY: return "Тяжёлый ключ. Подойдёт к висячему замку.";
                case CROWBAR: return "Можно отжать ржавый засов.";
                case NOTE: return "«Код от кодового замка: 137»";
                case MILK: return "Маруся будет спать как убитая.";
                case BATTERY: return "Ещё немного света в этом аду.";
                case BANDAGE: return "Перевязать ногу. +Здоровье.";
            }
            return "";
        }
        public static int Icon(int id)
        {
            switch (id)
            {
                case KEY: return 0;
                case CROWBAR: return 1;
                case NOTE: return 2;
                case MILK: return 3;
                case BATTERY: return 4;
                case BANDAGE: return 5;
            }
            return 7;
        }
    }

    public class Level
    {
        public int W, H, Id;
        public string Name;
        public byte[] Map;
        public List<Container> Containers = new List<Container>();
        public List<Door> Doors = new List<Door>();
        public List<HideSpot> Hides = new List<HideSpot>();
        public List<Deco> Decos = new List<Deco>();
        public List<Trap> Traps = new List<Trap>();
        public double SpawnX, SpawnY, SpawnA;
        public string[] RoomNames;
        public byte[] TrapGrid;

        public void RebuildTrapGrid()
        {
            if (TrapGrid == null) TrapGrid = new byte[W * H];
            for (int i = 0; i < TrapGrid.Length; i++) TrapGrid[i] = 0;
            for (int i = 0; i < Traps.Count; i++)
            {
                Trap t = Traps[i];
                int tx = (int)t.X, ty = (int)t.Y;
                if (tx >= 0 && ty >= 0 && tx < W && ty < H) TrapGrid[ty * W + tx] = (byte)(t.Armed ? 2 : 1);
            }
        }

        public Level(int w, int h, int id, string name)
        {
            W = w; H = h; Id = id; Name = name;
            Map = new byte[w * h];
            for (int i = 0; i < Map.Length; i++) Map[i] = T.BRICK;
        }

        public byte Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return T.BRICK;
            return Map[y * W + x];
        }

        public void Set(int x, int y, byte t)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Map[y * W + x] = t;
        }

        public void FillRect(int x0, int y0, int x1, int y1, byte t)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) Set(x, y, t);
        }

        public bool SolidAt(double wx, double wy)
        {
            int x = (int)Math.Floor(wx), y = (int)Math.Floor(wy);
            byte t = Get(x, y);
            if (T.Solid[t])
            {
                if (t == T.DOOR)
                {
                    Door d = DoorAt(x, y);
                    if (d != null && d.Open) return false;
                }
                return true;
            }
            return false;
        }

        public Door DoorAt(int x, int y)
        {
            for (int i = 0; i < Doors.Count; i++)
                if (Doors[i].X == x && Doors[i].Y == y) return Doors[i];
            return null;
        }

        public Container ContainerAt(int x, int y)
        {
            for (int i = 0; i < Containers.Count; i++)
                if (Containers[i].X == x && Containers[i].Y == y) return Containers[i];
            return null;
        }

        public HideSpot HideAt(int x, int y)
        {
            for (int i = 0; i < Hides.Count; i++)
                if (Hides[i].X == x && Hides[i].Y == y) return Hides[i];
            return null;
        }

        public Trap TrapNear(double x, double y, double r)
        {
            for (int i = 0; i < Traps.Count; i++)
            {
                Trap t = Traps[i];
                if (!t.Armed) continue;
                if (Math.Abs(t.X - x) < r && Math.Abs(t.Y - y) < r) return t;
            }
            return null;
        }

        public bool LineOfSight(double x0, double y0, double x1, double y1)
        {
            double dx = x1 - x0, dy = y1 - y0;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.0001) return true;
            int steps = (int)(dist * 12) + 1;
            double sx = dx / steps, sy = dy / steps;
            double x = x0, y = y0;
            for (int i = 0; i < steps; i++)
            {
                x += sx; y += sy;
                if (SolidAt(x, y)) return false;
            }
            return true;
        }

        public bool WalkableTile(int x, int y)
        {
            byte t = Get(x, y);
            if (!T.Solid[t]) return true;
            if (t == T.DOOR)
            {
                Door d = DoorAt(x, y);
                if (d != null && d.Open) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- A*
        public int[] PathX = new int[0];
        public int[] PathY = new int[0];
        int[] cameFrom;
        float[] gScore;
        bool[] closed;
        List<int> open;

        public List<int> FindPath(int sx, int sy, int tx, int ty, bool canOpenDoors)
        {
            int n = W * H;
            if (cameFrom == null || cameFrom.Length != n)
            {
                cameFrom = new int[n]; gScore = new float[n]; closed = new bool[n];
            }
            for (int i = 0; i < n; i++) { cameFrom[i] = -1; gScore[i] = float.MaxValue; closed[i] = false; }
            if (open == null) open = new List<int>(); else open.Clear();
            if (sx < 0 || sy < 0 || sx >= W || sy >= H) return null;
            if (tx < 0 || ty < 0 || tx >= W || ty >= H) return null;
            int start = sy * W + sx, goal = ty * W + tx;
            // if target is solid (furniture) walk to adjacent instead
            if (!WalkableTile(tx, ty))
            {
                int best = -1; double bd = 1e9;
                for (int y = ty - 1; y <= ty + 1; y++)
                    for (int x = tx - 1; x <= tx + 1; x++)
                    {
                        if (!WalkableTile(x, y)) continue;
                        double d = M.Dist(x, y, tx, ty);
                        if (d < bd) { bd = d; best = y * W + x; }
                    }
                if (best < 0) return null;
                goal = best; tx = goal % W; ty = goal / W;
            }
            gScore[start] = 0;
            open.Add(start);
            int guard = 0;
            while (open.Count > 0 && guard++ < 20000)
            {
                int bi = 0; float bf = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int c = open[i];
                    int cx = c % W, cy = c / W;
                    float f = gScore[c] + Math.Abs(cx - tx) + Math.Abs(cy - ty);
                    if (f < bf) { bf = f; bi = i; }
                }
                int cur = open[bi];
                open.RemoveAt(bi);
                if (cur == goal)
                {
                    List<int> path = new List<int>();
                    int c = cur;
                    while (c != -1) { path.Add(c); c = cameFrom[c]; }
                    path.Reverse();
                    return path;
                }
                closed[cur] = true;
                int ux = cur % W, uy = cur / W;
                for (int k = 0; k < 4; k++)
                {
                    int nx = ux + (k == 0 ? 1 : k == 1 ? -1 : 0);
                    int ny = uy + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                    int ni = ny * W + nx;
                    if (closed[ni]) continue;
                    if (!WalkableTile(nx, ny))
                    {
                        // doors count as passable for granny (she opens them)
                        byte t = Get(nx, ny);
                        if (!(canOpenDoors && t == T.DOOR)) continue;
                    }
                    float cost = 1f;
                    byte tt = Get(nx, ny);
                    if (tt == T.DOOR) cost += 1.5f;
                    float ng = gScore[cur] + cost;
                    if (ng < gScore[ni])
                    {
                        gScore[ni] = ng;
                        cameFrom[ni] = cur;
                        bool inOpen = false;
                        for (int i = 0; i < open.Count; i++) if (open[i] == ni) { inOpen = true; break; }
                        if (!inOpen) open.Add(ni);
                    }
                }
            }
            return null;
        }

        public bool Reachable(int sx, int sy, int tx, int ty, bool canOpenDoors)
        {
            List<int> p = FindPath(sx, sy, tx, ty, canOpenDoors);
            return p != null && p.Count > 0;
        }
    }

    public static class WorldBuilder
    {
        public static Rng R;
        public static Level House, Attic;

        public static int GlobalSeed;

        public static void Build(int seed)
        {
            GlobalSeed = seed;
            R = new Rng(seed);
            House = BuildHouseLevel();
            Attic = BuildAtticLevel();
            DistributeItems();
            Log.W("world built: house containers=" + House.Containers.Count + " attic containers=" + Attic.Containers.Count);
        }

        static void Outline(Level lv, int x0, int y0, int x1, int y1, byte wall)
        {
            for (int x = x0; x <= x1; x++) { lv.Set(x, y0, wall); lv.Set(x, y1, wall); }
            for (int y = y0; y <= y1; y++) { lv.Set(x0, y, wall); lv.Set(x1, y, wall); }
        }

        static void WallpaperPass(Level lv)
        {
            for (int y = 1; y < lv.H - 1; y++)
                for (int x = 1; x < lv.W - 1; x++)
                {
                    if (lv.Get(x, y) != T.BRICK) continue;
                    bool near = lv.Get(x - 1, y) == T.FLOOR || lv.Get(x + 1, y) == T.FLOOR ||
                                lv.Get(x, y - 1) == T.FLOOR || lv.Get(x, y + 1) == T.FLOOR ||
                                lv.Get(x - 1, y) == T.RUG || lv.Get(x + 1, y) == T.RUG ||
                                lv.Get(x, y - 1) == T.RUG || lv.Get(x, y + 1) == T.RUG;
                    if (near) lv.Set(x, y, T.WP);
                }
        }

        static Level BuildHouseLevel()
        {
            Level lv = new Level(32, 32, 0, "Дом Гренни");
            lv.RoomNames = new string[] { "Спальня", "Кухня", "Коридор", "Гостиная", "Кладовая", "Кабинет", "Ванная", "Прихожая" };

            // ---- rooms (carve)  x2..11 / hall 13..18 / x20..29
            lv.FillRect(2, 2, 11, 12, T.FLOOR);       // bedroom
            lv.FillRect(20, 2, 29, 12, T.FLOOR);      // kitchen
            lv.FillRect(14, 2, 16, 13, T.FLOOR);      // vertical corridor
            lv.FillRect(2, 14, 29, 16, T.FLOOR);      // horizontal corridor
            lv.FillRect(2, 18, 11, 24, T.FLOOR);      // living
            lv.FillRect(20, 18, 29, 24, T.FLOOR);     // storage
            lv.FillRect(2, 26, 11, 29, T.FLOOR);      // study
            lv.FillRect(20, 26, 29, 29, T.FLOOR);     // bathroom
            lv.FillRect(13, 17, 18, 29, T.FLOOR);     // hall

            WallpaperPass(lv);
            // room-specific wall finishes
            for (int y = 1; y < lv.H - 1; y++)
                for (int x = 1; x < lv.W - 1; x++)
                {
                    byte tb = lv.Get(x, y);
                    if (tb != T.WP && tb != T.BRICK) continue;
                    if (y >= 25) lv.Set(x, y, T.TILEW);                       // bathroom
                    else if (y >= 17 && x >= 19) lv.Set(x, y, T.PANEL);       // storage
                    else if (y >= 17 && x <= 12) lv.Set(x, y, T.PANEL);       // living / study
                }

            // ---- windows on outer walls
            int[] wxt = { 5, 8, 22, 26 };
            for (int i = 0; i < wxt.Length; i++) { lv.Set(wxt[i], 1, T.WINDOW); lv.Set(wxt[i], 30, T.WINDOW); }
            lv.Set(1, 8, T.WINDOW); lv.Set(1, 20, T.WINDOW); lv.Set(30, 6, T.WINDOW); lv.Set(30, 21, T.WINDOW);

            // ---- doors
            AddDoor(lv, 6, 13, false);
            AddDoor(lv, 24, 13, false);
            AddDoor(lv, 7, 17, false);
            AddDoor(lv, 24, 17, false);
            AddDoor(lv, 7, 25, false);
            AddDoor(lv, 24, 25, false);
            // front door (south wall of the hall)
            lv.Set(15, 30, T.FRONT);
            lv.Set(16, 30, T.WP);
            Door front = new Door(15, 30, true); front.Open = false; lv.Doors.Add(front);

            // ---- attic hatch
            lv.Set(15, 3, T.HOLE);

            // ---- bedroom
            lv.Set(10, 3, T.WARDROBE);
            lv.Set(10, 9, T.CABINET);
            lv.Set(3, 11, T.CABINET);
            Deco bed = new Deco(5.6, 4.0, TexLib.Furn[0], 0.62, 1);
            bed.RadiusX = 1.35; bed.RadiusY = 0.62; bed.Kind = 0;
            lv.Decos.Add(bed);
            lv.FillRect(4, 6, 8, 8, T.RUG);
            lv.Hides.Add(new HideSpot(10, 3, 10, 4, false, "Шкаф в спальне"));
            lv.Hides.Add(new HideSpot(6, 5, 6, 6, true, "Под кроватью"));

            // ---- kitchen
            lv.Set(20, 2, T.FRIDGE);
            lv.Set(21, 2, T.STOVE);
            lv.Set(29, 2, T.CABINET);
            lv.Set(29, 11, T.CABINET);
            lv.Set(25, 2, T.SHELF);
            Deco ktable = new Deco(24.5, 7.0, TexLib.Furn[1], 0.9, 1);
            ktable.RadiusX = 1.1; ktable.RadiusY = 0.7; ktable.Kind = 1;
            lv.Decos.Add(ktable);

            // ---- living
            lv.Set(2, 18, T.SHELF);
            lv.Set(2, 23, T.CABINET);
            lv.Set(11, 23, T.CRATE);
            lv.FillRect(5, 20, 8, 22, T.RUG);
            Deco sofa = new Deco(5.0, 23.2, TexLib.Furn[2], 0.75, 1);
            sofa.RadiusX = 1.5; sofa.RadiusY = 0.62; sofa.Kind = 2;
            lv.Decos.Add(sofa);

            // ---- storage
            lv.Set(28, 18, T.CRATE);
            lv.Set(28, 19, T.CRATE);
            lv.Set(20, 18, T.SHELF);
            lv.Set(23, 24, T.CRATE);
            lv.Set(28, 23, T.CABINET);
            Deco crate2 = new Deco(22.5, 21.5, TexLib.Furn[3], 0.8, 1);
            crate2.RadiusX = 0.7; crate2.RadiusY = 0.55; crate2.Kind = 3;
            lv.Decos.Add(crate2);

            // ---- study
            lv.Set(2, 26, T.BOOKS);
            lv.Set(3, 26, T.SHELF);
            lv.Set(10, 29, T.CABINET);
            lv.Set(7, 29, T.CRATE);
            Deco desk = new Deco(8.5, 28.2, TexLib.Furn[4], 0.85, 1);
            desk.RadiusX = 1.1; desk.RadiusY = 0.6; desk.Kind = 4;
            lv.Decos.Add(desk);

            // ---- bathroom
            lv.Set(28, 26, T.CABINET);
            lv.Set(21, 29, T.TOWELS);
            lv.Set(24, 29, T.MIRROR);

            // ---- hall
            lv.Set(18, 20, T.WARDROBE);
            lv.Set(12, 26, T.PORTRAIT);
            lv.Set(12, 22, T.PORTRAIT);
            lv.Set(19, 27, T.PORTRAIT);
            lv.Set(17, 28, T.CRATE);
            lv.Hides.Add(new HideSpot(18, 20, 17, 20, false, "Шкаф в прихожей"));

            // ---- containers
            AddContainer(lv, 10, 9, "Комод в спальне");
            AddContainer(lv, 3, 11, "Ночной столик");
            AddContainer(lv, 29, 2, "Кухонный шкаф");
            AddContainer(lv, 29, 11, "Кухонный шкаф");
            AddContainer(lv, 20, 2, "Холодильник");
            AddContainer(lv, 2, 23, "Комод в гостиной");
            AddContainer(lv, 28, 23, "Ящик в кладовой");
            AddContainer(lv, 10, 29, "Ящик стола");
            AddContainer(lv, 28, 26, "Шкафчик в ванной");
            AddContainer(lv, 7, 29, "Старый ящик");

            lv.SpawnX = 15.5; lv.SpawnY = 27.5; lv.SpawnA = -M.PI / 2;
            return lv;
        }

        static void AddDoor(Level lv, int x, int y, bool front)
        {
            lv.Set(x, y, T.DOOR);
            lv.Doors.Add(new Door(x, y, front));
        }

        static void AddContainer(Level lv, int x, int y, string label)
        {
            byte tb = lv.Get(x, y);
            if (tb == T.WARDROBE || tb == T.SHELF || tb == T.CRATE || tb == T.CABINET || tb == T.FRIDGE)
                lv.Containers.Add(new Container(x, y, tb, label));
        }

        static Level BuildAtticLevel()
        {
            Level lv = new Level(24, 24, 1, "Чердак");
            lv.FillRect(2, 2, 21, 21, T.FLOOR);
            // beams / posts
            lv.Set(10, 11, T.ATTICWOOD);
            lv.Set(15, 11, T.ATTICWOOD);
            lv.Set(10, 16, T.ATTICWOOD);
            lv.Set(15, 16, T.ATTICWOOD);
            lv.FillRect(2, 2, 3, 21, T.ATTICWOOD);
            // walls
            for (int y = 1; y < lv.H - 1; y++)
                for (int x = 1; x < lv.W - 1; x++)
                    if (lv.Get(x, y) == T.BRICK) lv.Set(x, y, T.ATTIC);
            lv.Set(11, 1, T.WINDOW);
            // hatch down
            lv.Set(4, 20, T.HOLE);
            // clutter
            lv.Set(20, 7, T.CAGE);
            lv.Set(20, 8, T.CAGE);
            lv.Set(17, 3, T.CRATE);
            lv.Set(18, 3, T.CRATE);
            lv.Set(3, 3, T.SHELF);
            lv.Set(7, 19, T.CRATE);
            lv.Set(14, 19, T.CRATE);
            lv.Set(15, 19, T.CRATE);
            lv.Set(19, 14, T.PORTRAIT);
            lv.Set(9, 4, T.CABINET);      // old chest with the note
            lv.Set(6, 8, T.WARDROBE);
            lv.Hides.Add(new HideSpot(6, 8, 6, 9, false, "Старый шкаф"));
            lv.Set(16, 16, T.CRATE);
            lv.Set(13, 6, T.CRATE);
            lv.FillRect(11, 8, 14, 9, T.RUG);

            lv.Containers.Add(new Container(9, 4, T.CABINET, "Старый сундук"));
            lv.Containers.Add(new Container(3, 3, T.SHELF, "Полка с хламом"));
            lv.Containers.Add(new Container(16, 16, T.CRATE, "Ящик на чердаке"));

            lv.SpawnX = 4.5; lv.SpawnY = 19.0; lv.SpawnA = -M.PI / 2;
            return lv;
        }

        static void DistributeItems()
        {
            // required: key, crowbar, note ; extras: milk, battery, bandage
            List<Container> pool = new List<Container>();
            for (int i = 0; i < House.Containers.Count; i++) pool.Add(House.Containers[i]);
            R.Shuffle(pool);

            // the note is always in the attic chest (cat guards it)
            Container chest = Attic.Containers[0];
            chest.ItemId = Items.NOTE;

            List<int> req = new List<int>();
            req.Add(Items.KEY); req.Add(Items.CROWBAR);
            req.Add(Items.MILK); req.Add(Items.BATTERY); req.Add(Items.BANDAGE);
            R.Shuffle(req);

            int idx = 0;
            for (int i = 0; i < req.Count && i < pool.Count; i++)
                pool[i].ItemId = req[i];
            idx = req.Count;
            // one extra random item somewhere
            if (idx < pool.Count)
            {
                int[] extra = { Items.BANDAGE, Items.BATTERY, Items.MILK };
                pool[idx].ItemId = extra[R.Range(0, extra.Length)];
            }
            // attic extras
            if (Attic.Containers.Count > 1) Attic.Containers[1].ItemId = Items.BANDAGE;
            if (Attic.Containers.Count > 2) Attic.Containers[2].ItemId = Items.BATTERY;

            Log.W("items: key=" + FindItem(House, Items.KEY) + " crowbar=" + FindItem(House, Items.CROWBAR));
        }

        static string FindItem(Level lv, int id)
        {
            for (int i = 0; i < lv.Containers.Count; i++)
                if (lv.Containers[i].ItemId == id) return lv.Containers[i].Label;
            return "-";
        }
    }
}
