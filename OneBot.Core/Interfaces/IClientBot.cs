using OneBot.Base;
using OneBot.Models;

namespace OneBot.Interfaces
{
    public interface IClientBot<TUser> where TUser : BaseUser
    {
        public int Id { get; }

        public void RegisterUpdateHadler(Action<ReceptionClient<TUser>> action);
        public void UnregisterUpdateHadler(Action<ReceptionClient<TUser>> action);

        public Task Run(CancellationToken token = default);

        public ButtonSearch? GetIndexButton(ReceptionClient<TUser> client, ButtonsSend buttonsSend);
    }
}
