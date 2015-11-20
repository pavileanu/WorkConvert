public class clsSlotSummary
{

    public int Given;
    public int PreInstalledTaken;
    public int taken;
    public float TotalCapacity; //working variable to total of the quantities of all options taking this slot type (For sumarising HDD and Memory Capacity)
    public float TotalRedundantCapacity; //Put in for PSU's but Paul suggests this will be a common thing for other items...
    public clsUnit CapacityUnit;

    public clsSlotSummary(int Given, int Taken, int totalCapacity, int totalRedundantCapacity)
    {

        this.Given = Given;
        this.taken = Taken;
        this.TotalCapacity = totalCapacity;
        this.TotalRedundantCapacity = totalRedundantCapacity;
    }


}