using OneBot.Base;
using OneBot.Models;

namespace OneBot.Interfaces
{
    public interface IClientBot<TUser> where TUser : BaseUser
    {
        public int Id { get; }

        public void RegisterUpdateHadler(Action<UpdateContext<TUser>> action);
        public void UnregisterUpdateHadler(Action<UpdateContext<TUser>> action);

        public Task Run(CancellationToken token = default);

        public ButtonSearch? GetIndexButton(UpdateContext<TUser> context, ButtonsSend buttonsSend);
    }
}
