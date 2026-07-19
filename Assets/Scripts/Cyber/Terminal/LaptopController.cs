using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class LaptopController : MonoBehaviour
    {
        public static LaptopController Instance { get; private set; }

        [SerializeField] private float connectionRange = 3f;

        public bool IsConnected { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public TerminalController ConnectedTerminal { get; private set; }

        public event System.Action<TerminalController> OnConnected;
        public event System.Action OnDisconnected;

        public ConnectionResult Connect(TerminalController terminal)
        {
            if (IsConnected)
            {
                return new ConnectionResult
                {
                    Success = false,
                    ErrorMessage = "Already connected to a terminal."
                };
            }

            if (terminal == null)
            {
                return new ConnectionResult
                {
                    Success = false,
                    ErrorMessage = "No terminal to connect to."
                };
            }

            float dist = Vector3.Distance(transform.position, terminal.transform.position);
            if (dist > connectionRange)
            {
                return new ConnectionResult
                {
                    Success = false,
                    ErrorMessage = "Terminal out of range."
                };
            }

            ConnectedTerminal = terminal;
            IsConnected = true;
            OnConnected?.Invoke(terminal);
            return new ConnectionResult { Success = true };
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            if (ConnectedTerminal != null)
            {
                ConnectedTerminal.Disconnect();
            }

            ConnectedTerminal = null;
            IsConnected = false;
            OnDisconnected?.Invoke();
        }

        public void ForceDisconnect()
        {
            IsConnected = false;
            ConnectedTerminal = null;
            OnDisconnected?.Invoke();
        }

        void OnDisable()
        {
            if (IsConnected)
                ForceDisconnect();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsConnected)
                ForceDisconnect();
        }
    }

    public struct ConnectionResult
    {
        public bool Success;
        public string ErrorMessage;
    }
}
