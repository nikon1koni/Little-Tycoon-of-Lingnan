﻿﻿﻿﻿﻿// Cross-scene settlement data. Populated by GameManager when the round limit
// is reached, then read by EndSceneController in the "End" scene.
public static class GameResult
{
    public static int Score;
    public static int BuildingsPlaced;
    public static int DiceRolls;
    public static int GoldEarned;
    public static int Rounds;
    public static int MaxRounds;

    public static void Set(int score, int buildingsPlaced, int diceRolls, int goldEarned, int rounds, int maxRounds)
    {
        Score = score;
        BuildingsPlaced = buildingsPlaced;
        DiceRolls = diceRolls;
        GoldEarned = goldEarned;
        Rounds = rounds;
        MaxRounds = maxRounds;
    }
}
