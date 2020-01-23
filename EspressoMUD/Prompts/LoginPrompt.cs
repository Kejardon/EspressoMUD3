using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    /// <summary>
    /// Handles new connections that have not logged in yet.
    /// </summary>
    public class LoginPrompt : StandardHeldPrompt
    {
        protected string accountName;
        protected Account foundAccount;
        protected enum LoginState
        {
            FirstPrompt,
            AccountPassword,
            IsNewAccount,
            NewAccountPassword
        }
        protected LoginState currentSubState = LoginState.IsNewAccount;
        protected LoginState CurrentState
        {
            get
            {
                if (string.IsNullOrEmpty(accountName))
                {
                    return LoginState.FirstPrompt;
                }
                if (foundAccount != null && !string.IsNullOrEmpty(foundAccount.Password))
                {
                    return LoginState.AccountPassword;
                }
                return currentSubState;
            }
        }

        /// <summary>
        /// Start a login prompt for a given client.
        /// </summary>
        /// <param name="forClient">Client connection that is logging in.</param>
        public LoginPrompt(Client forClient) : base(null)
        {
            this.User = forClient;
        }

        /// <summary>
        /// Reset a user to logged in to just their account.
        /// </summary>
        /// <param name="forClient">Client connection that is logging in.</param>
        public static StandardHeldPrompt GetLoggedInPrompt(Client forClient)
        {
            if (forClient.LoggedInAccount == null)
            {
                throw new Exception("Error: User not logged into account to return to logged-in state.");
            }
            LoginPrompt parent = new LoginPrompt(forClient);
            LoggedInMenu child = new LoggedInMenu(parent);
            parent.NextPrompt = child;
            parent.Reset();
            return child;
        }

        /// <summary>
        /// Get the string 
        /// </summary>
        public override string PromptMessage
        {
            get
            {
                string header;
                switch (this.CurrentState)
                {
                    case LoginState.FirstPrompt:
                        header = "Account name: ";
                        break;
                    case LoginState.AccountPassword:
                        header = "Password: ";
                        break;
                    case LoginState.IsNewAccount:
                        header = "'" + this.accountName + "' is not an existing account. Do you want to create it? (y/N) ";
                        break;
                    case LoginState.NewAccountPassword:
                        header = "Enter a password for the account: ";
                        break;
                    default:
                        throw new Exception("Invalid Login state");
                }

                return header;
            }
        }

        protected override void InnerRespond(string userString)
        {
            switch (this.CurrentState)
            {
                case LoginState.FirstPrompt:
                    AccountObjects accounts = AccountObjects.Get();
                    accountName = userString;
                    foundAccount = accounts.GetAccount(userString); //Transition to either AccountPassword or IsNewAccount
                    User.sendMessage(this.PromptMessage);
                    break;
                case LoginState.AccountPassword:
                    if (Account.EncryptionMethod.Compare(userString, foundAccount.Password))
                    {
                        FinishLogin();
                    }
                    else
                    {
                        User.sendMessage("That password does not match the account's password.");
                        accountName = null; //Return to account name prompt.
                        User.sendMessage(this.PromptMessage);
                    }
                    break;
                case LoginState.IsNewAccount:
                    if (userString.Length > 0 && userString.Substring(0, 1).ToUpper() == "Y")
                    {
                        currentSubState = LoginState.NewAccountPassword;
                    }
                    else
                    {
                        accountName = null;
                    }
                    User.sendMessage(this.PromptMessage);
                    break;
                case LoginState.NewAccountPassword:
                    foundAccount = new Account();
                    foundAccount.Name = accountName;
                    foundAccount.Password = userString;
                    bool success = AccountObjects.Get().Add(foundAccount);
                    if (success)
                    {
                        foundAccount.Save();
                        FinishLogin();
                    }
                    else
                    {
                        User.sendMessage("Account creation failed.");
                        Reset();
                        User.sendMessage(this.PromptMessage);
                    }
                    break;
            }
        }
        private void Reset()
        {
            currentSubState = LoginState.IsNewAccount;
            foundAccount = null;
            accountName = null;
        }
        private void FinishLogin()
        {
            //TODO: Successful Log in with this user.
            User.LoggedInAccount = foundAccount;
            NextPrompt = new LoggedInMenu(this);
            Reset();
        }
    }
}
