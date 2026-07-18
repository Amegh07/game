using System.Collections.Generic;
using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class TerminalController : MonoBehaviour
    {
        [SerializeField] private string terminalID = "";
        [SerializeField] private TerminalConfig config;
        [SerializeField] private TerminalLog terminalLog;
        [SerializeField] private SecurityConsoleUI consoleUI;

        private LaptopController connectedLaptop;
        private NetworkSession currentSession;
        private List<TerminalActionEntry> availableActions;
        private bool isLocked;

        public NetworkSession CurrentSession => currentSession;
        public TerminalConfig Config => config;
        public TerminalLog Log => terminalLog;
        public bool IsConnected => connectedLaptop != null && currentSession != null && currentSession.IsActive;
        public bool IsLocked => isLocked;
        public string TerminalID => terminalID;

        public event System.Action<NetworkSession> OnSessionStarted;
        public event System.Action OnSessionEnded;
        public event System.Action<TerminalLogEntry> OnActionExecuted;

        void Start()
        {
            if (terminalLog == null)
                terminalLog = GetComponent<TerminalLog>();

            if (terminalLog == null)
                terminalLog = gameObject.AddComponent<TerminalLog>();

            availableActions = new List<TerminalActionEntry>();
            if (config != null)
            {
                availableActions.AddRange(config.actions);
            }
        }

        void OnDestroy()
        {
            Disconnect();
        }

        public ConnectionResult Connect(LaptopController laptop)
        {
            if (isLocked)
            {
                return new ConnectionResult
                {
                    Success = false,
                    ErrorMessage = "Terminal is locked."
                };
            }

            if (config == null)
            {
                return new ConnectionResult
                {
                    Success = false,
                    ErrorMessage = "Terminal not configured."
                };
            }

            connectedLaptop = laptop;
            return new ConnectionResult { Success = true };
        }

        public void Disconnect()
        {
            if (currentSession != null)
            {
                currentSession.IsActive = false;
                currentSession = null;
            }

            connectedLaptop = null;

            if (consoleUI != null && consoleUI.isActiveAndEnabled)
                consoleUI.Hide();

            OnSessionEnded?.Invoke();
        }

        public AuthenticationResult Authenticate(string credentialID)
        {
            if (config == null)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Terminal not configured."
                };
            }

            AuthenticationResult authResult;

            if (AuthenticationService.Instance != null)
            {
                authResult = AuthenticationService.Instance.Authenticate(credentialID);
            }
            else
            {
                authResult = new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Authentication system unavailable."
                };
            }

            if (authResult.Success)
            {
                AuthorizationResult authzResult;
                if (AuthorizationService.Instance != null)
                {
                    authzResult = AuthorizationService.Instance.Authorize(
                        authResult.Role, terminalID, config.minimumRole);
                }
                else
                {
                    authzResult = new AuthorizationResult
                    {
                        Authorized = true,
                        Role = authResult.Role,
                        Permissions = new HashSet<string>()
                    };
                }

                if (authzResult.Authorized)
                {
                    currentSession = new NetworkSession
                    {
                        UserName = authResult.UserName,
                        Role = authResult.Role,
                        IsAuthenticated = true,
                        ConnectedTerminalID = terminalID,
                        Permissions = authzResult.Permissions ?? new HashSet<string>(),
                        StartTime = System.DateTime.UtcNow,
                        IsActive = true
                    };

                    LogSessionEvent("Session started");
                    OnSessionStarted?.Invoke(currentSession);

                    if (consoleUI != null)
                        consoleUI.Show(this);

                    return new AuthenticationResult
                    {
                        Success = true,
                        Role = authResult.Role,
                        UserName = authResult.UserName
                    };
                }
                else
                {
                    LogSessionEvent($"Authorization failed: {authzResult.ErrorMessage}");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = authzResult.ErrorMessage
                    };
                }
            }

            LogSessionEvent($"Authentication failed: {authResult.ErrorMessage}");
            return authResult;
        }

        public void LockTerminal()
        {
            isLocked = true;
            Disconnect();
        }

        public void UnlockTerminal()
        {
            isLocked = false;
        }

        public bool CanExecuteAction(TerminalActionEntry actionEntry)
        {
            if (currentSession == null || !currentSession.IsActive) return false;
            if (string.IsNullOrEmpty(actionEntry.requiredPermission)) return true;
            return currentSession.HasPermission(actionEntry.requiredPermission);
        }

        public void ExecuteAction(TerminalActionEntry actionEntry)
        {
            if (currentSession == null || !currentSession.IsActive)
            {
                LogAction("Action blocked", "No active session", false);
                return;
            }

            if (!CanExecuteAction(actionEntry))
            {
                LogAction(actionEntry.displayName, "Permission denied", false);
                return;
            }

            string resultMessage;
            bool success = ActionExecutor.ExecuteAction(actionEntry, currentSession, out resultMessage);

            LogAction(actionEntry.displayName, resultMessage, success);

            if (consoleUI != null)
                consoleUI.RefreshLog();
        }

        public List<TerminalActionEntry> GetAuthorizedActions()
        {
            List<TerminalActionEntry> authorized = new();
            foreach (var action in availableActions)
            {
                if (CanExecuteAction(action))
                    authorized.Add(action);
            }
            return authorized;
        }

        private void LogAction(string action, string result, bool success)
        {
            string userName = currentSession?.UserName ?? "Unknown";
            UserRole role = currentSession?.Role ?? UserRole.Guest;

            var entry = new TerminalLogEntry(userName, role, action, result, success);
            if (terminalLog != null)
                terminalLog.AddEntry(entry);

            OnActionExecuted?.Invoke(entry);
        }

        private void LogSessionEvent(string message)
        {
            if (terminalLog != null)
            {
                terminalLog.AddEntry("SYSTEM", UserRole.Guest, "Session", message, true);
            }
        }
    }
}
