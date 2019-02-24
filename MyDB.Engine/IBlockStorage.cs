
namespace MyDB.Engine
{
    public interface IBlockStorage
    {
        /// <summary>
        /// Find block in storage by provided id.
        /// </summary>
        IBlock Find(long blockId);

        /// <summary>
        /// Create new block in storage.
        /// </summary>
        IBlock Create();
    }
}
