using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using SocialDeductionGame;

public enum RoleType
{
    Townfolk,
    Assassin,
    CourProphet, // Seer
    Alchemist,   // Witch
    Jester       // Fool
}

public enum TeamType
{
    Town,
    Assassin,
    Neutral
}

public class Player
{
    public string playerName;
    public RoleType role;
    public TeamType team;
    public bool isAlive = true;

    // Alchemist ability usage
    public bool hasSaved = false;
    public bool hasKilled = false;

    public Player(string name)
    {
        playerName = name;
        role = RoleType.Townfolk;
        team = TeamType.Town;
    }
}

public class GameManager : MonoBehaviour
{
    public List<Player> players = new List<Player>();

    [Header("Voting")]
    public GameObject voteButtonPrefab;
    public Transform voteButtonContainer;
    public float voteTime = 30f; // 30 seconds for voting

    private Dictionary<Player, int> voteCount;
    private Dictionary<Player, Player> playerLastVote; // Tracks each player's current vote

    [Header("UI")]
    public TMP_Text gameMessageText;

    private Player humanPlayer; // single human player

    // Night temp variables
    private Player assassinTarget;
    private Player alchemistSaveTarget;
    private Player alchemistKillTarget;
    private Player prophetTarget;

    void Start()
    {
        // Example players
        players.Add(new Player("Player 1"));
        players.Add(new Player("Player 2"));
        players.Add(new Player("Player 3"));
        players.Add(new Player("Player 4"));
        players.Add(new Player("Player 5"));

        humanPlayer = players[0]; // first player is human

        AssignRoles();
        StartCoroutine(GameLoop());
    }

    void AssignRoles()
    {
        // Reset
        foreach (var p in players)
        {
            p.role = RoleType.Townfolk;
            p.team = TeamType.Town;
        }

        // Random assignment
        int assassinIndex = Random.Range(0, players.Count);
        players[assassinIndex].role = RoleType.Assassin;
        players[assassinIndex].team = TeamType.Assassin;

        int prophetIndex;
        do { prophetIndex = Random.Range(0, players.Count); } while (prophetIndex == assassinIndex);
        players[prophetIndex].role = RoleType.CourProphet;
        players[prophetIndex].team = TeamType.Town;

        int alchemistIndex;
        do { alchemistIndex = Random.Range(0, players.Count); } while (alchemistIndex == assassinIndex || alchemistIndex == prophetIndex);
        players[alchemistIndex].role = RoleType.Alchemist;
        players[alchemistIndex].team = TeamType.Town;

        int jesterIndex;
        do { jesterIndex = Random.Range(0, players.Count); } while (jesterIndex == assassinIndex || jesterIndex == prophetIndex || jesterIndex == alchemistIndex);
        players[jesterIndex].role = RoleType.Jester;
        players[jesterIndex].team = TeamType.Neutral;

        gameMessageText.text = "Roles assigned! Game starts...";
    }

    IEnumerator GameLoop()
    {
        while (!CheckWin())
        {
            // NIGHT PHASE
            yield return StartCoroutine(NightPhase());

            if (CheckWin()) break;

            // DAY PHASE
            yield return StartCoroutine(DayPhase());
        }

        gameMessageText.text += "\nGame Over!";
    }

    IEnumerator NightPhase()
    {
        // Reset night targets
        assassinTarget = null;
        alchemistSaveTarget = null;
        alchemistKillTarget = null;
        prophetTarget = null;

        // Phase 0: Night intro
        gameMessageText.text = "Night falls. Shadows creep over the village...";
        yield return new WaitForSeconds(2f);

        // Phase 1: Assassin
        Player assassin = players.Find(p => p.role == RoleType.Assassin && p.isAlive);
        if (assassin != null)
        {
            gameMessageText.text = "Assassin, choose your victim from the shadows...";
            yield return new WaitForSeconds(2f);
            // Example random choice for now (replace with player input)
            List<Player> targets = players.FindAll(p => p.isAlive && p != assassin);
            assassinTarget = targets[Random.Range(0, targets.Count)];
        }

        // Phase 2: Alchemist
        Player alchemist = players.Find(p => p.role == RoleType.Alchemist && p.isAlive);
        if (alchemist != null)
        {
            gameMessageText.text = "Alchemist, choose someone to protect or eliminate...";
            yield return new WaitForSeconds(2f);
            // Example: randomly save if ability not used
            if (!alchemist.hasSaved)
            {
                List<Player> saveOptions = players.FindAll(p => p.isAlive);
                alchemistSaveTarget = saveOptions[Random.Range(0, saveOptions.Count)];
                alchemist.hasSaved = true;
            }
            // Example: randomly kill if ability not used
            if (!alchemist.hasKilled)
            {
                List<Player> killOptions = players.FindAll(p => p.isAlive && p != alchemist);
                alchemistKillTarget = killOptions[Random.Range(0, killOptions.Count)];
                alchemist.hasKilled = true;
            }
        }

        // Phase 3: Cour Prophet
        Player prophet = players.Find(p => p.role == RoleType.CourProphet && p.isAlive);
        if (prophet != null)
        {
            gameMessageText.text = "Cour Prophet, choose a player to investigate...";
            yield return new WaitForSeconds(2f);
            List<Player> targets = players.FindAll(p => p.isAlive && p != prophet);
            prophetTarget = targets[Random.Range(0, targets.Count)];
            gameMessageText.text = $"Cour Prophet sees that {prophetTarget.playerName} is a {prophetTarget.role}.";
            yield return new WaitForSeconds(2f);
        }

        // Apply night results
        if (assassinTarget != null)
        {
            if (assassinTarget != alchemistSaveTarget)
            {
                assassinTarget.isAlive = false;
                gameMessageText.text = $"Night kill: {assassinTarget.playerName} ({assassinTarget.role})";
            }
            else
            {
                gameMessageText.text = $"{assassinTarget.playerName} was saved by the Alchemist!";
            }
            yield return new WaitForSeconds(2f);
        }

        if (alchemistKillTarget != null)
        {
            alchemistKillTarget.isAlive = false;
            gameMessageText.text = $"Alchemist eliminated {alchemistKillTarget.playerName} ({alchemistKillTarget.role})";
            yield return new WaitForSeconds(2f);
        }

        gameMessageText.text = "Night ends...";
        yield return new WaitForSeconds(2f);
    }

    IEnumerator DayPhase()
    {
        gameMessageText.text = "Day Phase: Vote for a player!";
        playerLastVote = new Dictionary<Player, Player>(); // reset last votes
        ShowVoteButtons();

        float timer = voteTime;
        while (timer > 0f && voteButtonContainer.childCount > 0)
        {
            gameMessageText.text = $"Day Phase: Vote for a player! Time left: {timer:F0}s";
            timer -= Time.deltaTime;
            UpdateVoteCountsUI(); // update vote counts in real-time
            yield return null;
        }

        // Count votes
        voteCount = new Dictionary<Player, int>();
        foreach (var vote in playerLastVote.Values)
        {
            if (!voteCount.ContainsKey(vote))
                voteCount[vote] = 0;
            voteCount[vote]++;
        }

        Player votedOut = ResolveVotes();
        if (votedOut != null)
        {
            votedOut.isAlive = false;
            gameMessageText.text = $"Voted out: {votedOut.playerName} (Role: {votedOut.role})";

            // Check Jester win
            if (votedOut.role == RoleType.Jester)
            {
                gameMessageText.text = $"Jester ({votedOut.playerName}) wins immediately!";
            }
        }
        else
        {
            gameMessageText.text = "No one was voted out!";
        }

        ClearButtons();
        yield return new WaitForSeconds(2f);
    }

    void ShowVoteButtons()
    {
        ClearButtons();

        foreach (Player p in players)
        {
            if (!p.isAlive) continue;

            GameObject btnObj = Instantiate(voteButtonPrefab, voteButtonContainer);
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            txt.text = $"{p.playerName} (0)";

            // Store player reference on button
            VoteButtonData data = btnObj.AddComponent<VoteButtonData>();
            data.player = p;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => RegisterVote(data.player));
        }
    }

    void RegisterVote(Player target)
    {
        playerLastVote[humanPlayer] = target;
        UpdateVoteCountsUI();
    }

    void UpdateVoteCountsUI()
    {
        var currentVoteCount = new Dictionary<Player, int>();
        foreach (var vote in playerLastVote.Values)
        {
            if (!currentVoteCount.ContainsKey(vote))
                currentVoteCount[vote] = 0;
            currentVoteCount[vote]++;
        }

        foreach (Transform btn in voteButtonContainer)
        {
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            VoteButtonData data = btn.GetComponent<VoteButtonData>();
            Player p = data.player;
            int count = currentVoteCount.ContainsKey(p) ? currentVoteCount[p] : 0;
            txt.text = $"{p.playerName} ({count})";
        }
    }

    Player ResolveVotes()
    {
        if (voteCount.Count == 0) return null;

        int maxVotes = voteCount.Values.Max();
        var topPlayers = voteCount.Where(x => x.Value == maxVotes).Select(x => x.Key).ToList();

        return topPlayers[Random.Range(0, topPlayers.Count)];
    }

    void ClearButtons()
    {
        foreach (Transform child in voteButtonContainer)
            Destroy(child.gameObject);
    }

    bool CheckWin()
    {
        int town = 0;
        int assassins = 0;

        foreach (Player p in players)
        {
            if (!p.isAlive) continue;
            if (p.team == TeamType.Town) town++;
            if (p.team == TeamType.Assassin) assassins++;
        }

        if (assassins == 0)
        {
            gameMessageText.text = "Town wins!";
            return true;
        }

        if (assassins >= town)
        {
            gameMessageText.text = "Assassins win!";
            return true;
        }

        return false;
    }
}

public class VoteButtonData : MonoBehaviour
{
    public Player player;
}