using UnityEngine;

[CreateAssetMenu(fileName = "EntityMetadata", menuName = "Platformer/Character Metadata")]
public sealed class EntityMetadata : ScriptableObject
{
    [Header("Character Metadata")]
    [SerializeField] private string characterName;
    [SerializeField] private string characterDescription;
    [SerializeField] private GameplayFaction faction;
    [SerializeField] private int enemyLevel;
    
    [Header("Enemy Rewards")]
    [SerializeField] private int goldReward;
    [SerializeField] private float expReward;
    
    
    public string CharacterName => characterName;
    public string CharacterDescription => characterDescription;
    public GameplayFaction Faction => faction;
    public int EnemyLevel => enemyLevel;
    public int GoldReward => goldReward;
    public float ExpReward => expReward;
    

}
