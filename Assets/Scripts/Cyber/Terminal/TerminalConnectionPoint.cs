using UnityEngine;

namespace MuseumHeist.Cyber
{
    [RequireComponent(typeof(BoxCollider))]
    public class TerminalConnectionPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private TerminalController terminalController;

        void Start()
        {
            if (terminalController == null)
                terminalController = GetComponentInParent<TerminalController>();

            BoxCollider bc = GetComponent<BoxCollider>();
            bc.isTrigger = true;
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;

            if (terminalController.CurrentSession != null && terminalController.CurrentSession.IsActive)
            {
                NoiseManager.EmitAt(transform.position, 4f, 0.2f, NoiseType.Interaction, gameObject);
                terminalController.Disconnect();
                if (LaptopController.Instance != null)
                    LaptopController.Instance.ForceDisconnect();
                return;
            }

            if (!TryFindLaptop(out LaptopController laptop))
            {
                Debug.Log("TerminalConnectionPoint: Laptop required to connect.");
                return;
            }

            ConnectionResult result = laptop.Connect(terminalController);

            if (result.Success)
            {
                NoiseManager.EmitAt(transform.position, 4f, 0.2f, NoiseType.Interaction, gameObject);
                terminalController.Connect(laptop);
            }
            else
            {
                Debug.Log($"TerminalConnectionPoint: {result.ErrorMessage}");
            }
        }

        public bool CanInteract(PlayerController player)
        {
            if (terminalController == null) return false;
            return true;
        }

        public string GetInteractionPrompt()
        {
            if (terminalController == null) return "Terminal";
            if (terminalController.CurrentSession != null && terminalController.CurrentSession.IsActive)
                return "Disconnect Laptop";
            return "Connect Laptop";
        }

        public void OnFocus() { }
        public void OnLoseFocus() { }

        private static bool TryFindLaptop(out LaptopController laptop)
        {
            laptop = LaptopController.Instance;
            return laptop != null;
        }
    }
}
