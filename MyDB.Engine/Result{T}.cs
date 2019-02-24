using System.Diagnostics.Contracts;

namespace MyDB.Engine
{
    public class Result<T> : Result
    {
        private T _value;

        public T Value
        {
            get
            {
                Contract.Requires(Success);

                return _value;
            }

            private set { _value = value; }
        }

        protected internal Result(T value, bool success, string error)
            : base(success, error)
        {
            Contract.Requires(value != null || !success);

            Value = value;
        }
    }
}
