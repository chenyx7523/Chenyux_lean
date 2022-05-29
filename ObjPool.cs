
//模板


#region    对象池模板
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePool : MonoBehaviour
{
    //方法改为静态，方便调用
    public static GamePool gamepool;

    //自定义池子
    private Dictionary<string, Queue<GameObject>> pool;

    //默认池子
    //private Queue<GameObject> queue;

    //设置一个池子的容量，即最大值
    private int maxCount = int.MaxValue;

    //设置最大值读取类型
    public int MaxCount
    {

        get { return maxCount; }
        set { maxCount = Mathf.Clamp(value, 0, int.MaxValue); }
    }

    //单例类，生成对象池字典，包含名字，对象。
    private void Awake()
    {
        gamepool = this;
        //pool.Clear();
        pool = new Dictionary<string, Queue<GameObject>>();
    }


    /// <summary>
    /// 从池中获取物体
    /// </summary>
    /// <param name="go">需要取得的物体</param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public GameObject GetObjInPool(GameObject obj, Vector3 position, Quaternion rotation)
    {
        //如果未初始化过 初始化池
        if (!pool.ContainsKey(obj.name))
        {
            //添加新的字典型。
            pool.Add(obj.name, new Queue<GameObject>());
        }
        //如果池空了就创建新物体（即obj中的名字键值为空）
        if (pool[obj.name].Count == 0)
        {
            //再次实例化新的
            GameObject newObject = Instantiate(obj, position, rotation);
            newObject.name = obj.name;
            /*
            确认名字一样，防止系统加一个(clone),或序号累加之类的
            实际上为了更健全可以给每一个物体加一个key，防止对象的name一样但实际上不同
             */
            return newObject;
        }

        //获取物体
        GameObject nextObject = pool[obj.name].Dequeue();
        
        nextObject.transform.position = position;   
        nextObject.transform.rotation = rotation;
        nextObject.SetActive(true);

        return nextObject;
    }

    // 把物体放回池里
    public void PutObjInPool(GameObject obj, float m_time)
    {
        //如果溢出，则删除，不再放入池子
        if (pool[obj.name].Count >= MaxCount)
            Destroy(obj, m_time);
        else
            //反之则继续放入池子
            StartCoroutine(ReInPool(obj, m_time));
    }

    
    private IEnumerator ReInPool(GameObject obj, float m_time)
    {
        //延迟十秒后执行后续
        yield return new WaitForSeconds(m_time);
        obj.SetActive(false);
        pool[obj.name].Enqueue(obj);
    }

    //物体预热/预加载
    public void LoadObjInPool(GameObject obj, int number)
    {
        if(!pool.ContainsKey(obj.name))
        {
            pool.Add(obj.name,new Queue<GameObject>());
        }
        for(int i = 0; i< number; i++)
        {
            //提前Instantiate一些对象
            GameObject newObject = Instantiate(obj);
            newObject.name= obj.name;
            newObject.SetActive(false);
            pool[obj.name].Enqueue(newObject);//Enqueue   保持它不销毁


        }



    }


}

#endregion


#region    调用对象池中物体

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class poolManager : MonoBehaviour
{
    public GameObject testobj;
    public GameObject testobj2;
    private int num;
    private int numZ;

    private void Start()
    {
        num = 0;
        //预热物体
        GamePool.gamepool.LoadObjInPool(testobj, 100);
        Pooluse();

        GamePool.gamepool.LoadObjInPool(testobj2, 100);
        Pooluse2();


    }


    //使用对象池调用坦克
    public void Pooluse()
    {
        StartCoroutine(CreateObj());
    }

    private IEnumerator CreateObj()
    {
        for (int i =0;i<25;i++)
        {
            
            if (num >= 25)
            {
                numZ += 5;
                 num = 0;
            }
            for(int q = 0;q<=5; q++)
            {
                num += 5;
                int x = num;
                float y = 0.52f;
                int z = numZ;
                GameObject testObjcet = GamePool.gamepool.GetObjInPool(testobj, new Vector3(x, y, z), Quaternion.identity);
                GamePool.gamepool.PutObjInPool(testObjcet,10f);
            }
            yield return null;
        }
    }


    public void Pooluse2()
    {
        StartCoroutine(CreateObj2());
    }

    private IEnumerator CreateObj2()
    {
        for (int i = 0; i < 200; i++)
        {

            if (num >= 66)
            {
                numZ += 3;
                num = 0;
            }
            for (int q = 0; q <= 22; q++)
            {
                num += 3;
                int x = num+100;
                float y = 0.52f;
                int z = numZ;
                GameObject testObjcet = GamePool.gamepool.GetObjInPool(testobj2, new Vector3(x, y, z), Quaternion.identity);
            }
            yield return null;
        }
    }
}

#endregion



#region 

#endregion



#region 

#endregion



#region 

#endregion



#region 

#endregion


#region 

#endregion



#region 

#endregion