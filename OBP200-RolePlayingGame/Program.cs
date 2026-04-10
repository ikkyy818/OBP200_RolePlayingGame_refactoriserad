using System.Text;
using System.Transactions;

namespace OBP200_RolePlayingGame;


class Program
{
   
    // ======= Globalt tillstånd  =======

       static Player ? CurrentPlayer;
   

    // Rum: [type, label]
    // types: battle, treasure, shop, rest, boss
    static List<IRoom> Rooms = new List<IRoom>();

    // Fiendemallar: [type, name, HP, ATK, DEF, XPReward, GoldReward]
    static List<string[]> EnemyTemplates = new List<string[]>();

    // Status för kartan
    static int CurrentRoomIndex = 0;

    // Random
    static Random Rng = new Random();

    // ======= Main =======

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        InitEnemyTemplates();

        while (true)
        {
            ShowMainMenu();
            Console.Write("Välj: ");
            var choice = (Console.ReadLine() ?? "").Trim();

            if (choice == "1")
            {
                StartNewGame();
                RunGameLoop();
            }
            else if (choice == "2")
            {
                Console.WriteLine("Avslutar...");
                return;
            }
            else
            {
                Console.WriteLine("Ogiltigt val.");
            }

            Console.WriteLine();
        }
    }

    // ======= Meny & Init =======

    static void ShowMainMenu()
    {
        Console.WriteLine("=== Text-RPG ===");
        Console.WriteLine("1. Nytt spel");
        Console.WriteLine("2. Avsluta");
    }

    static void StartNewGame()
    {
        Console.Write("Ange namn: ");
        var name = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) name = "Namnlös";


        Console.WriteLine("Välj klass: 1) Warrior  2) Mage  3) Rogue");
        Console.Write("Val: ");
        var k = (Console.ReadLine() ?? "").Trim();        

        /* //string cls = "Warrior";
        int hp = 0, maxhp = 0, atk = 0, def = 0;
        int potions = 0, gold = 0;
         */
        string cls;

    switch (k)
    {
        case "1":
            cls = "Warrior";
            break;

        case "2":
            cls = "Mage";
            break;

        case "3":
            cls = "Rogue";
            break;

        default:
            cls = "Warrior";
            break;
    }
    CurrentPlayer = new Player(name, cls);

   
    

        // Initiera karta (linjärt äventyr)
        Rooms.Clear();

        Rooms.Add(new BattleRoom("Skogsstig", () => EnterRoom("battle")));
        Rooms.Add(new TreasureRoom("Gammal kista", DoTreasure));
        Rooms.Add(new ShopRoom("Vandrande köpman", DoShop));
        Rooms.Add(new BattleRoom("Grottans mynning", () => EnterRoom("battle")));
        Rooms.Add(new RestRoom("Lägereld", DoRest));
        Rooms.Add(new BattleRoom("Grottans djup", () => EnterRoom("battle")));
        Rooms.Add(new BossRoom("Urdraken", () => EnterRoom("boss")));

        CurrentRoomIndex = 0;

        Console.WriteLine($"Välkommen, {name} the {cls}!");
        ShowStatus();
        
    
       
    
    }

    static void RunGameLoop()
    {
        while (true)
        {
            var room = Rooms[CurrentRoomIndex];

            Console.WriteLine($"Rum {CurrentRoomIndex + 1}: {room.Name} ({room.Type})");

            bool continueAdventure = room.Enter();
            
            if (IsPlayerDead())
            {
                Console.WriteLine("Du har stupat... Spelet över.");
                break;
            }
            
            if (!continueAdventure)
            {
                Console.WriteLine("Du lämnar äventyret för nu.");
                break;
            }

            CurrentRoomIndex++;
            
            if (CurrentRoomIndex >= Rooms.Count)
            {
                Console.WriteLine();
                Console.WriteLine("Du har klarat äventyret!");
                break;
            }
            
            Console.WriteLine();
            Console.WriteLine("[C] Fortsätt     [Q] Avsluta till huvudmeny");
            Console.Write("Val: ");
            var post = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            if (post == "Q")
            {
                Console.WriteLine("Tillbaka till huvudmenyn.");
                break;
            }

            Console.WriteLine();
        }
    }

    // ======= Rumshantering =======

    static bool EnterRoom(string type)
    {
        switch ((type ?? "battle").Trim())
        {
            case "battle":
                return DoBattle(isBoss: false);
            case "boss":
                return DoBattle(isBoss: true);
            case "treasure":
                return DoTreasure();
            case "shop":
                return DoShop();
            case "rest":
                return DoRest();
            default:
                Console.WriteLine("Du vandrar vidare...");
                return true;
        }
    }

    // ======= Strid =======

    static bool DoBattle(bool isBoss)
    {
        var enemy = GenerateEnemy(isBoss);
        Console.WriteLine($"En {enemy[1]} dyker upp! (HP {enemy[2]}, ATK {enemy[3]}, DEF {enemy[4]})");

        int enemyHp = ParseInt(enemy[2], 10);
        int enemyAtk = ParseInt(enemy[3], 3);
        int enemyDef = ParseInt(enemy[4], 0);

        

        while (enemyHp > 0 && !IsPlayerDead())
        {
            Console.WriteLine();
            ShowStatus();
            Console.WriteLine($"Fiende: {enemy[1]} HP={enemyHp}");
            Console.WriteLine("[A] Attack   [X] Special   [P] Dryck   [R] Fly");
            if (isBoss) Console.WriteLine("(Du kan inte fly från en boss!)");
            Console.Write("Val: ");

            var cmd = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            if (cmd == "A")
            {
                int damage = CalculatePlayerDamage(enemyDef);
                enemyHp -= damage;
                Console.WriteLine($"Du slog {enemy[1]} för {damage} skada.");
            }
            else if (cmd == "X")
            {
                int special = UseClassSpecial(enemyDef, isBoss);
                enemyHp -= special;
                Console.WriteLine($"Special! {enemy[1]} tar {special} skada.");
            }
            else if (cmd == "P")
            {
                UsePotion();
            }
            else if (cmd == "R" && !isBoss)
            {
                if (TryRunAway())
                {
                    Console.WriteLine("Du flydde!");
                    return true; // fortsätt äventyr
                }
                else
                {
                    Console.WriteLine("Misslyckad flykt!");
                }
            }
            else
            {
                Console.WriteLine("Du tvekar...");
            }

            if (enemyHp <= 0) break;

            // Fiendens tur
            int enemyDamage = CalculateEnemyDamage(enemyAtk);
            ApplyDamageToPlayer(enemyDamage);
            Console.WriteLine($"{enemy[1]} anfaller och gör {enemyDamage} skada!");
        }

        if (IsPlayerDead())
        {
            return false; // avsluta äventyr
        }

        // Vinstrapporter, XP, guld, loot
        int xpReward = ParseInt(enemy[5], 5);
        int goldReward = ParseInt(enemy[6], 3);

        AddPlayerXp(xpReward);
        AddPlayerGold(goldReward);

        Console.WriteLine($"Seger! +{xpReward} XP, +{goldReward} guld.");
        MaybeDropLoot(enemy[1]);

        return true;
    }

    static string[] GenerateEnemy(bool isBoss)
    {
        if (isBoss)
        {
            // Boss-mall
            return new[] { "boss", "Urdraken", "55", "9", "4", "30", "50" };
        }
        else
        {
            // Slumpa bland templates
            var template = EnemyTemplates[Rng.Next(EnemyTemplates.Count)];
            
            // Slmumpmässig justering av stats
            int hp = ParseInt(template[2], 10) + Rng.Next(-1, 3);
            int atk = ParseInt(template[3], 3) + Rng.Next(0, 2);
            int def = ParseInt(template[4], 0) + Rng.Next(0, 2);
            int xp = ParseInt(template[5], 4) + Rng.Next(0, 3);
            int gold = ParseInt(template[6], 2) + Rng.Next(0, 3);
            return new[] { template[0], template[1], hp.ToString(), atk.ToString(), def.ToString(), xp.ToString(), gold.ToString() };
        }
    }

    static void InitEnemyTemplates()
    {
        EnemyTemplates.Clear();
        EnemyTemplates.Add(new[] { "beast", "Vildsvin", "18", "4", "1", "6", "4" });
        EnemyTemplates.Add(new[] { "undead", "Skelett", "20", "5", "2", "7", "5" });
        EnemyTemplates.Add(new[] { "bandit", "Bandit", "16", "6", "1", "8", "6" });
        EnemyTemplates.Add(new[] { "slime", "Geléslem", "14", "3", "0", "5", "3" });
    }

    static int CalculatePlayerDamage(int enemyDef)
    {
        int atk = CurrentPlayer!.Attack;
        string cls = CurrentPlayer.ClassName;

        // Beräkna grundskada
        int baseDmg = Math.Max(1, atk - (enemyDef / 2));
        int roll = Rng.Next(0, 3); // liten variation

        switch (cls.Trim())
        {
            case "Warrior":
                baseDmg += 1; // warrior buff
                break;
            case "Mage":
                baseDmg += 2; // mage buff
                break;
            case "Rogue":
                baseDmg += (Rng.NextDouble() < 0.2) ? 4 : 0; // rogue crit-chans
                break;
            default:
                baseDmg += 0;
                break;
        }

        return Math.Max(1, baseDmg + roll);
    }

    static int UseClassSpecial(int enemyDef, bool vsBoss)
    {
        string cls = CurrentPlayer!.ClassName ?? "Warrior";
        int specialDmg = 0;

        // Hantering av specialförmågor
        if (cls == "Warrior")
        {
            // Heavy Strike: hög skada men självskada
            Console.WriteLine("Warrior använder Heavy Strike!");
            int atk = CurrentPlayer!.Attack;
            specialDmg = Math.Max(2, atk + 3 - enemyDef);
            ApplyDamageToPlayer(2); // självskada
        }
        else if (cls == "Mage")
        {
            // Fireball: stor skada, kostar guld
            int gold = CurrentPlayer!.Gold;
            if (gold >= 3)
            {
                Console.WriteLine("Mage kastar Fireball!");                
                CurrentPlayer!.SpendGold(3);
                int atk = CurrentPlayer!.Attack;
                specialDmg = Math.Max(3, atk + 5 - (enemyDef / 2));
            }
            else
            {
                Console.WriteLine("Inte tillräckligt med guld för att kasta Fireball (kostar 3).");
                specialDmg = 0;
            }
        }
        else if (cls == "Rogue")
        {
            // Backstab: chans att ignorera försvar, hög risk/hög belöning
            if (Rng.NextDouble() < 0.5)
            {
                Console.WriteLine("Rogue utför en lyckad Backstab!");
                int atk = CurrentPlayer!.Attack;
                specialDmg = Math.Max(4, atk + 6);
            }
            else
            {
                Console.WriteLine("Backstab misslyckades!");
                specialDmg = 1;
            }
        }
        else
        {
            specialDmg = 0;
        }

        // Dämpa skada mot bossen
        if (vsBoss)
        {
            specialDmg = (int)Math.Round(specialDmg * 0.8);
        }

        return Math.Max(0, specialDmg);
    }

    static int CalculateEnemyDamage(int enemyAtk)
    {
        int def = CurrentPlayer!.Defense;

        int roll = Rng.Next(0, 3);

        int dmg = Math.Max(1, enemyAtk - (def / 2)) + roll;

        // liten chans för reducerad skada
        if (Rng.NextDouble() < 0.1)
            dmg = Math.Max(1, dmg - 2);

        return dmg;
    }

    static void ApplyDamageToPlayer(int dmg)
    {
        CurrentPlayer!.TakeDamage(dmg);        
    }

    static void UsePotion()
    {
        CurrentPlayer!.UsePotion();       
    }

    static bool TryRunAway()
    {
        // Flyktschans baserad på karaktärsklass
        string cls = CurrentPlayer!.ClassName ?? "Warrior";
        double chance = 0.25;
        if (cls == "Rogue") chance = 0.5;
        if (cls == "Mage") chance = 0.35;
        return Rng.NextDouble() < chance;
    }

    static bool IsPlayerDead()
    {
        return CurrentPlayer!.IsDead();
    }

    static void AddPlayerXp(int amount)
    {  
        CurrentPlayer!.AddXP(amount);        
    }

    static void AddPlayerGold(int amount)
    {  
        CurrentPlayer!.AddGold(amount);
    }
  

    static void MaybeDropLoot(string enemyName)
    {
        // Enkel loot-regel
        if (Rng.NextDouble() < 0.35)
        {
            string item = "Minor Gem";
            if (enemyName.Contains("Urdraken")) item = "Dragon Scale";

                CurrentPlayer!.AddItem(item);

            Console.WriteLine($"Föremål hittat: {item} (lagt i din väska)");
        }
    }

    // ======= Rumshändelser =======

   static bool DoTreasure()
    {
        Console.WriteLine("Du hittar en gammal kista...");

        if (Rng.NextDouble() < 0.5)
        {
            int gold = Rng.Next(8, 15);
            AddPlayerGold(gold);
            Console.WriteLine($"Kistan innehåller {gold} guld!");
        }
        else
        {
            var items = new[] { "Iron Dagger", "Oak Staff", "Leather Vest", "Healing Herb" };
            string found = items[Rng.Next(items.Length)];

            CurrentPlayer!.AddItem(found);

            Console.WriteLine($"Du plockar upp: {found}");
        }

        return true;
    }

    
    

    static bool DoShop()
{
    Console.WriteLine("En vandrande köpman erbjuder sina varor:");

    while (true)
    {
        Console.WriteLine($"Guld: {CurrentPlayer!.Gold} | Drycker: {CurrentPlayer.Potions}");
        Console.WriteLine("1) Köp dryck (10 guld)");
        Console.WriteLine("2) Köp vapen (+2 ATK) (25 guld)");
        Console.WriteLine("3) Köp rustning (+2 DEF) (25 guld)");
        Console.WriteLine("4) Sälj alla 'Minor Gem' (+5 guld/st)");
        Console.WriteLine("5) Lämna butiken");
        Console.Write("Val: ");

        var val = (Console.ReadLine() ?? "").Trim();

        if (val == "1")
        {
            TryBuy(10, () => CurrentPlayer!.AddPotion(1), "Du köper en dryck.");
        }
        else if (val == "2")
        {
            TryBuy(25, () => CurrentPlayer!.IncreaseAttack(2), "Du köper ett bättre vapen.");
        }
        else if (val == "3")
        {
            TryBuy(25, () => CurrentPlayer!.IncreaseDefense(2), "Du köper bättre rustning.");
        }
        else if (val == "4")
        {
            SellMinorGems();
        }
        else if (val == "5")
        {
            Console.WriteLine("Du säger adjö till köpmannen.");
            break;
        }
        else
        {
            Console.WriteLine("Köpmannen förstår inte ditt val.");
        }
    }

    return true;
}

    static void TryBuy(int cost, Action apply, string successMsg)
    {
        
        if (CurrentPlayer!.SpendGold(cost))
        {          
            apply();
            Console.WriteLine(successMsg);
        }
        else
        {
            Console.WriteLine("Du har inte råd.");
        }
    }

    static void SellMinorGems()
{
    int goldEarned = CurrentPlayer!.SellMinorGems();

    if (goldEarned == 0)
    {
        Console.WriteLine("Inga 'Minor Gem' i väskan.");
    }
    else
    {
        Console.WriteLine($"Du säljer {goldEarned / 5} st Minor Gem för {goldEarned} guld.");
    }
}

    static bool DoRest()
    {
        Console.WriteLine("Du slår läger och vilar.");
        CurrentPlayer!.FullHeal();
        Console.WriteLine("HP återställt till max.");
        return true;
    }

    // ======= Status =======

    static void ShowStatus()
    {
        CurrentPlayer?.ShowStatus();
        CurrentPlayer?.ShowInventory();
    }
    
    // ======= Hjälpmetoder =======

    static int ParseInt(string s, int fallback)
    {
        try
        {
            int value = Convert.ToInt32(s);
            return value;
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
