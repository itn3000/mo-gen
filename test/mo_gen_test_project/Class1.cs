using System;
using MagicOnion;
using MagicOnion.Server;

namespace mo_gen_test_project
{
    public interface IMyService : IService<IMyService>
    {
        UnaryResult<int> SumAsync(int x, int y);
    }
    public class MyService : ServiceBase<IMyService>, IMyService
    {
        public async UnaryResult<int> SumAsync(int x, int y)
        {
            return x + y;
        }
    }
}
