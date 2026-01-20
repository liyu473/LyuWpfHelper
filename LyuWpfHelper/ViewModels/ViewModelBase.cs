using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace LyuWpfHelper.ViewModels;

/// <summary>
/// ViewModel 基类
/// 继承自 ObservableObject，集成了 Messenger 信使功能
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// 信使实例，用于发送和接收消息
    /// </summary>
    protected IMessenger Messenger { get; }

    /// <summary>
    /// 构造函数，使用默认的 WeakReferenceMessenger
    /// </summary>
    protected ViewModelBase() : this(WeakReferenceMessenger.Default)
    {
    }

    /// <summary>
    /// 构造函数，允许注入自定义的 Messenger 实例
    /// </summary>
    /// <param name="messenger">信使实例</param>
    protected ViewModelBase(IMessenger messenger)
    {
        Messenger = messenger;
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="message">消息实例</param>
    protected void Send<TMessage>(TMessage message) where TMessage : class
    {
        Messenger.Send(message);
    }

    /// <summary>
    /// 发送消息到指定频道
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">频道令牌类型</typeparam>
    /// <param name="message">消息实例</param>
    /// <param name="token">频道令牌</param>
    protected void Send<TMessage, TToken>(TMessage message, TToken token) 
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        Messenger.Send(message, token);
    }

    /// <summary>
    /// 注册消息接收器
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    protected void Register<TMessage>(MessageHandler<object, TMessage> handler) 
        where TMessage : class
    {
        Messenger.Register(this, handler);
    }

    /// <summary>
    /// 注册指定频道的消息接收器
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">频道令牌类型</typeparam>
    /// <param name="token">频道令牌</param>
    /// <param name="handler">消息处理器</param>
    protected void Register<TMessage, TToken>(TToken token, MessageHandler<object, TMessage> handler) 
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        Messenger.Register(this, token, handler);
    }

    /// <summary>
    /// 取消注册所有消息接收器
    /// </summary>
    protected void UnregisterAll()
    {
        Messenger.UnregisterAll(this);
    }

    /// <summary>
    /// 取消注册指定类型的消息接收器
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    protected void Unregister<TMessage>() where TMessage : class
    {
        Messenger.Unregister<TMessage>(this);
    }

    /// <summary>
    /// 取消注册指定频道的消息接收器
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TToken">频道令牌类型</typeparam>
    /// <param name="token">频道令牌</param>
    protected void Unregister<TMessage, TToken>(TToken token) 
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        Messenger.Unregister<TMessage, TToken>(this, token);
    }
}
