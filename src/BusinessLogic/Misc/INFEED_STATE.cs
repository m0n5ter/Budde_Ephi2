namespace PharmaProject.BusinessLogic.Misc
{
    public enum INFEED_STATE
    {
        NONE,
        WAITING_FOR_NEW_ORDER,
        WAITING_FOR_PACKAGE,
        WAITING_FOR_SCAN,
        WAITING_FOR_WEIGHING,
        DISPATCHING
    }
}