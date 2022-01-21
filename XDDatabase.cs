using PokemonPRNG.LCG32.GCLCG;

/// <summary>
/// Implantation of https://github.com/yatsuna827/XDDatabase/blob/c649b77010ab81117057f2a02cc902931b663efc/XDDatabase/Program.cs#L219-L297
/// </summary>
static class XDDatabase
{
    public static (uint pIndex, uint eIndex, uint HP, uint seed) Generate(uint seed)
    {
        seed.Advance(); // PlaynerName
        var playerTeamIndex = seed.GetRand() % 5;
        var enemyTeamIndex = seed.GetRand() % 5;

        var hp = new uint[4];

        seed.Advance(); 
        uint EnemyTSV = seed.GetRand() ^ seed.GetRand();

        // 相手1匹目
        seed.Advance(); // dummyPID
        seed.Advance(); // dummyPID
        hp[0] = seed.GetRand() & 0x1F;
        seed.Advance(); // SCD
        seed.Advance(); // Ability
        while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ EnemyTSV) >= 8) break; }
        hp[0] += seed.GenerateEVs() / 4;

        // 相手2匹目
        seed.Advance();
        seed.Advance();
        hp[1] = seed.GetRand() & 0x1F;
        seed.Advance();
        seed.Advance();
        while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ EnemyTSV) >= 8) break; }
        hp[1] += seed.GenerateEVs() / 4;

        seed.Advance(); 
        uint PlayerTSV = seed.GetRand() ^ seed.GetRand();

        // プレイヤー1匹目
        seed.Advance();
        seed.Advance();
        hp[2] = seed.GetRand() & 0x1F;
        seed.Advance();
        seed.Advance();
        while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ PlayerTSV) >= 8) break; }
        hp[2] += seed.GenerateEVs() / 4;

        // プレイヤー2匹目
        seed.Advance();
        seed.Advance();
        hp[3] = seed.GetRand() & 0x1F;
        seed.Advance();
        seed.Advance();
        while (true) { if ((seed.GetRand() ^ seed.GetRand() ^ PlayerTSV) >= 8) break; }
        hp[3] += seed.GenerateEVs() / 4;

        return (playerTeamIndex, enemyTeamIndex, (hp[0]<<24) + (hp[1] << 16) + (hp[2] << 8) + (hp[3]), seed);
    }
    private static uint GenerateEVs(ref this uint seed)
    {
        var EVs = new byte[6];
        int sumEV = 0;
        for (var i = 0; i < 101; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                byte ev = (byte)(seed.GetRand() & 0xFF);
                EVs[j] += ev;
            }
            sumEV = EVs.Sum(_ => _);

            if (sumEV == 510) return EVs[0];
            if (sumEV <= 490) continue;
            if (sumEV < 530) break;
            if (i != 100) EVs = new byte[6];
        }
        var k = 0;
        while (sumEV != 510)
        {
            if (sumEV < 510 && EVs[k] < 255) { EVs[k]++; sumEV++; }
            if (sumEV > 510 && EVs[k] != 0) { EVs[k]--; sumEV--; }
            k = (k + 1) % 6;
        }
        return EVs[0];
    }
}