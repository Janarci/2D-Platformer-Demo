using UnityEngine;

public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private InputSystemPlayerInputSource inputSystemInputSource;
    [SerializeField] private MonoBehaviour customInputSource;
    [SerializeField] private CharacterStateMachine characterStateMachine;
    
    private IPlayerInputSource activeInputSource;
    private bool warnedAboutMissingInput;

    private void Awake()
    {
        ResolveReferences();
        activeInputSource = ResolveInputSource();
    }

    private void Update()
    {
        if (playerMovement == null)
        {
            return;
        }

        activeInputSource ??= ResolveInputSource();

        if (activeInputSource == null)
        {
            WarnMissingInputSourceOnce();
            return;
        }

        var movementInput = activeInputSource.ReadMovementInput();
        var combatInput = activeInputSource.ReadCombatInput();
        
        playerMovement.SetInput(movementInput);

        if (characterStateMachine)
        {
            characterStateMachine.SetMovementAimInput(movementInput.Move);
            characterStateMachine.SetCombatInput(combatInput);
        }
    }

    public void SetInputSource(IPlayerInputSource inputSource)
    {
        activeInputSource = inputSource;
        warnedAboutMissingInput = false;
    }

    private void ResolveReferences()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (characterStateMachine == null)
        {
            characterStateMachine = GetComponent<CharacterStateMachine>();
        }

        if (inputSystemInputSource == null)
        {
            inputSystemInputSource = GetComponent<InputSystemPlayerInputSource>();
        }
    }

    private IPlayerInputSource ResolveInputSource()
    {
        if (customInputSource is IPlayerInputSource customSource)
        {
            return customSource;
        }

        return inputSystemInputSource != null
            ? inputSystemInputSource
            : GetComponent<InputSystemPlayerInputSource>();
    }

    private void WarnMissingInputSourceOnce()
    {
        if (warnedAboutMissingInput)
        {
            return;
        }

        warnedAboutMissingInput = true;
        Debug.LogWarning($"{nameof(PlayerController)} could not resolve an input source.", this);
    }
}
