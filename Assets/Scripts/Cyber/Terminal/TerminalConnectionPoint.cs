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

        public void Interact()
        {
            if (terminalController == null)
            {
                Debug.LogWarning("TerminalConnectionPoint: No TerminalController assigned.");
                return;
            }

            if (!TryFindLaptop(out LaptopController laptop))
            {
                Debug.Log("TerminalConnectionPoint: Laptop required to connect.");
                return;
            }

            if (terminalController.CurrentSession != null && terminalController.CurrentSession.IsActive)
            {
                terminalController.Disconnect();
                laptop.ForceDisconnect();
                return;
            }

            ConnectionResult result = laptop.Connect(terminalController);

            if (result.Success)
            {
                terminalController.Connect(laptop);
            }
            else
            {
                Debug.Log($"TerminalConnectionPoint: {result.ErrorMessage}");
            }
        }

        private static bool TryFindLaptop(out LaptopController laptop)
        {
            laptop = LaptopController.Instance;
            return laptop != null;
        }
    }
}
