namespace Loxodon.Framework.Views
{
    public interface IUpdateSystem
    {
        /// <summary>
        /// Do action when update call
        /// </summary>
        /// <param name="deltaTime"></param>
        void OnUpdate(float deltaTime);
    }
}
