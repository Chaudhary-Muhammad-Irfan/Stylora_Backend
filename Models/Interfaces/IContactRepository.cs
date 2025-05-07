namespace WebApplication1.Models.Interfaces
{
    public interface IContactRepository
    {
        void AddContact(Contact contact);
        int GetUnreadMessageCount();
        List<Contact> GetAllMessages();
        void MarkMessageAsRead();
    }
}
