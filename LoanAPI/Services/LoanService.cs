using LoanAPI.Domain;

namespace LoanAPI.Services
{
    public interface ILoanService
    {
        bool ChangeUserStatus(int id, bool isBlocked, out string message);
    }
    public class LoanService: ILoanService
    {

        private readonly LoanContext _context;

        public LoanService(LoanContext context)
        {
            _context = context;
        }

        public bool ChangeUserStatus(int id, bool isBlocked, out string message)
        {
            var user = _context.Users.Find(id);

            if (user == null)
            {
                message = "No User With Provided Id";
                return false;
            }

            user.IsBlocked = isBlocked;
            _context.SaveChanges();

            message = "Successfully Updated The User Status";
            return true;
        }

    }
}
