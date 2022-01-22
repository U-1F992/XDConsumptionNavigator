using PokemonPRNG.LCG32.GCLCG;

[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0")]

public class Program : ConsoleAppBase
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run<Program>(args);
    }
    
    [RootCommand]
    public void XDConsumptionNavigator(
        [Option("c", "Current seed.")] UInt32 currentSeed,
        [Option("t", "Target seed.")] UInt32 targetSeed,
        [Option("f", "Consumption forced.")] UInt32 consumptionForced = 0,
        [Option("b", "Number of consumption by opening the bag.")] UInt32 consumptionByBag = 0,
        [Option("", "Disable post-loading consumption methods.")] bool preload=false,
        [Option("", "List the parties.")] bool listing=false
    )
    {
        var targetSeed_org = targetSeed;

        // currentSeed から targetSeed までの消費数
        targetSeed = targetSeed.PrevSeed(consumptionForced);
        UInt32 totalConsumption = targetSeed.GetIndex(currentSeed);

        (long generateParties, long changeSetting, long writeReport, long openBag, long watchSteps) count = (0, 0, 0, 0, 0);

        // 最大のいますぐバトル生成数を取得する
        uint checkpointSeed = currentSeed;
        List<(uint pIndex, uint eIndex, uint HP, uint seed)> list = new List<(uint, uint, uint, uint)>();
        while (true)
        {
            list.Add(XDDatabase.Generate(checkpointSeed));
            if (totalConsumption < targetSeed.GetIndex(list.Last().seed))
            {
                // targetSeed までの消費数が totalConsumption を越えたら、targetSeed を追い越していると考えられる
                list.RemoveAt(list.Count - 1);
                break;
            }

            count.generateParties++;
            checkpointSeed = list.Last().seed;
        }

        long leftover = (long)targetSeed.GetIndex(checkpointSeed);
        if (preload)
        {
            // ロード前にぴったり消費するためには、いますぐバトル生成後の残り消費数が40で割り切れる必要がある
            while (leftover % 40 != 0)
            {
                if (list.Count == 0)
                {
                    Console.Error.Write("No way to reach {0} from {1} before loading.", Convert.ToString(targetSeed, 16), Convert.ToString(currentSeed, 16));
                    return;
                }
                list.RemoveAt(list.Count - 1);
                count.generateParties--;
                checkpointSeed = list.Count == 0 ? currentSeed : list.Last().seed;
                leftover = (long)targetSeed.GetIndex(checkpointSeed);
            }
        }
        else
        {
            // 持ち物消費が偶数であり残り消費数が63より少ない奇数である場合 (63より小さい奇数は消費できない)
            // 持ち物消費が奇数だが、残り消費数が持ち物消費より少ない場合 (持ち物消費より小さい奇数は消費できない)
            while (
                (consumptionByBag % 2 == 0 && leftover < 63 && leftover % 2 != 0) ||
                (leftover < consumptionByBag)
            )
            {
                if (list.Count == 0)
                {
                    Console.Error.Write("No way to reach {0} from {1}", Convert.ToString(targetSeed, 16), Convert.ToString(currentSeed, 16));
                    return;
                }
                list.RemoveAt(list.Count - 1);
                count.generateParties--;
                checkpointSeed = list.Count == 0 ? currentSeed : list.Last().seed;
                leftover = (long)targetSeed.GetIndex(checkpointSeed);
            }
        }
        
        if (preload)
        {
            count.changeSetting = (long)Math.Floor((decimal)((long)leftover / 40));
        }
        else
        {
            // レポート(63消費)
            count.writeReport = (long)Math.Floor((decimal)(leftover / 63));
            // 持ち物消費が偶数である場合、奇数の消費手段はレポートのみになる
            //
            // 残り消費数が奇数である場合、レポート回数は奇数である
            // 残り消費数が偶数である場合、偶数である
            if (((leftover % 2 != 0 && count.writeReport % 2 == 0) || (leftover % 2 == 0 && count.writeReport % 2 != 0)) && count.writeReport != 0)
            {
                count.writeReport--;
            }
            leftover -= 63 * count.writeReport;
            
            // 振動設定変更(40消費)
            count.changeSetting = (long)Math.Floor((decimal)(leftover / 40));
            leftover -= 40 * count.changeSetting;
            
            // 持ち物を開く(consumptionByBag消費)
            count.openBag = 0;
            if (consumptionByBag != 0)
            {
                count.openBag = (long)Math.Floor((decimal)(leftover / consumptionByBag));
                // 残り消費数が奇数である場合、持ち物を開く回数は奇数である
                // 残り消費数が偶数である場合、偶数である
                if (((leftover % 2 != 0 && count.openBag % 2 == 0) || (leftover % 2 == 0 && count.openBag % 2 != 0)) && count.openBag != 0)
                {
                    count.openBag--;
                }
                leftover -= consumptionByBag * count.openBag;
            }

            // 腰振り(2消費)
            count.watchSteps = leftover / 2;
        }

        Console.Write("{");
        PrintCount(count);

        if (listing)
        {
            if (list.Count != 0)
            {
                Console.Write(",\"list\":[");
                PrintListOfParties(list);
                Console.Write("]");
            }
        }
        Console.Write("}\n");

#if DEBUG
        for (int i = 0; i < count.generateParties; i++) currentSeed = XDDatabase.Generate(currentSeed).seed;
        System.Diagnostics.Debug.Assert(currentSeed.Advance((uint)(count.changeSetting * 40 + count.writeReport * 63 + count.openBag * consumptionByBag + count.watchSteps * 2 + consumptionForced)) == targetSeed_org);
#endif

        return;
    }

    private void PrintCount((long generateParties, long changeSetting, long writeReport, long openBag, long watchSteps) count)
    {
        Console.Write("\"generateParties\":{0},", count.generateParties);
        Console.Write("\"changeSetting\":{0},", count.changeSetting);
        Console.Write("\"writeReport\":{0},", count.writeReport);
        Console.Write("\"openBag\":{0},", count.openBag);
        Console.Write("\"watchSteps\":{0}", count.watchSteps);
    }
    private void PrintListOfParties(List<(uint pIndex, uint eIndex, uint HP, uint seed)> list)
    {
        for (long i = 0; i < list.Count; i++)
        {
            var item = list[(int)i];
            Console.Write(
                "{{\"index\":{0},\"seed\":{1},\"data\":{2}}}" + (i != list.Count - 1 ? "," : ""),
                i + 1,
                item.seed,
                ("{\"party\":["+item.pIndex+","+item.eIndex+"],\"hp\":["
                // https://github.com/yatsuna827/XDDatabase/blob/c649b77010ab81117057f2a02cc902931b663efc/XDDatabase/Program.cs#L137-L152
                +((new int[] {322, 310, 210, 320, 310})[item.pIndex] + ((item.HP & 0x0000ff00) >> 8))+","
                +((new int[] {340, 290, 620, 230, 310})[item.pIndex] + ((item.HP & 0x000000ff)))+","
                +((new int[] {290, 290, 290, 320, 270})[item.eIndex] + ((item.HP & 0xff000000) >> 24))+","
                +((new int[] {310, 270, 250, 270, 230})[item.eIndex] + ((item.HP & 0x00ff0000) >> 16))
                +"]}")
            );
        }
    }
}