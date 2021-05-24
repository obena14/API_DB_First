using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IRepository
    {
        void UseDatabase(string databaseName);
    }
    public interface IRepository<TEntity>
        : IRepository
    {
        IList<TEntity> GetAll();
        Task BulkInsertAsync(IList<TEntity> entities);
    }
    public interface IRepository<TEntity, TPrimaryKey>
        : IRepository<TEntity>
        where TEntity : RepositoryEntity
    {
        TEntity Get(TPrimaryKey id);
        TEntity Insert(TEntity entity);
        int Update(TEntity entity);
        int Delete(TEntity entity);
    }
    public interface IRepository<TEntity, TPrimaryKey, TPrimaryKey2>
     : IRepository<TEntity>
     where TEntity : RepositoryEntity
    {
        TEntity Get(TPrimaryKey id, TPrimaryKey2 id2);
        IList<TEntity> GetManyById1(TPrimaryKey id);
        IList<TEntity> GetManyById2(TPrimaryKey2 id2);
        TEntity Insert(TEntity entity);
        int Update(TEntity entity);
        int Upsert(TEntity entity);
        int Delete(TEntity entity);
    }
}
