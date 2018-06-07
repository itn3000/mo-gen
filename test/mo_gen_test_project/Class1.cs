using System;
using MagicOnion;
using MagicOnion.Server;

namespace mo_gen_test_project
{
    public class MyRes
    {
        public int X;
    }
    public interface IMyService1 : IService<IMyService1>
    {
        UnaryResult<int> SumAsync(int x, int y);
    }
    #if DEF1
    public interface IMyService2 : IService<IMyService2>
    {
        UnaryResult<int> Hoge(int x);
    }
    #endif
    #if DEF2
    public interface IMyService3 : IService<IMyService3>
    {
        UnaryResult<int> Piyo(int x);
    }
    #endif
}
