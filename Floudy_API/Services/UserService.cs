using Floudy.API.Storage;
using Floudy.API.Storage.Entities;
using System.Text.RegularExpressions;

namespace Floudy.API.Services
{
    public class UserService(UserRepository repository)
    {
        private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public UserEntity? GetByUsername(string username)
        {
            return repository.GetAll().FirstOrDefault(u => u.Username == username);
        }

        public UserEntity? GetByUsernameOrEmail(string identifier)
        {
            return repository.GetByUsernameOrEmail(identifier);
        }

        public bool EmailExists(string email)
        {
            return repository.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<UserEntity> GetAllNonAdmins()
        {
            return repository.GetAll().Where(u => u.RoleId != 1).ToList();
        }

        public bool UsernameExists(string username)
        {
            return repository.Any(u => u.Username == username);
        }

        public void RegisterUser(UserEntity user)
        {
            if (user.ID == 0) user.ID = DateTime.Now.Ticks;
            repository.Add(user);
        }

        public void UpdateUser(UserEntity user)
        {
            repository.Update(user);
        }

        public UserEntity? GetById(long id) => repository.GetById(id);

        public bool ValidateRegistration(string username, string email, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Email is required.";
                return false;
            }
            if (!EmailRegex.IsMatch(email))
            {
                errorMessage = "Invalid email format.";
                return false;
            }
            if (UsernameExists(username))
            {
                errorMessage = "Username already exists.";
                return false;
            }
            if (EmailExists(email))
            {
                errorMessage = "Email already exists.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool ValidateRecoveryCheck(string usernameOrEmail, out string? errorMessage, out UserEntity? user)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail))
            {
                errorMessage = "Username or email is required.";
                user = null;
                return false;
            }

            user = GetByUsernameOrEmail(usernameOrEmail);
            if (user == null)
            {
                errorMessage = "Username/email does not exist.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool ValidateResetPasswordInputs(string token, string password, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                errorMessage = "Token is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "New password is required.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool ValidateRename(string username, out string? errorMessage)
        {
            if (UsernameExists(username))
            {
                errorMessage = "Username already exists.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}

