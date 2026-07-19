public interface IInteractable
{
    void Interact(PlayerController player);
    bool CanInteract(PlayerController player);
    string GetInteractionPrompt();
    void OnFocus();
    void OnLoseFocus();
}
