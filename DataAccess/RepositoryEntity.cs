namespace DataAccess
{
    public class RepositoryEntity
    {
    }

    public class RepositoryEntity<TPrimaryKey>
        : RepositoryEntity
    {
    }
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    /// <typeparam name="TPrimaryKey2"></typeparam>
    public class RepositoryEntity<TPrimaryKey, TPrimaryKey2>
        : RepositoryEntity
    {
    }
}
