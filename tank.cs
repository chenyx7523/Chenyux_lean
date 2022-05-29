
//   TANK——BOOM！  项目经验


#region           游戏管理器
using Complete;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        /*[ReadOnly]*/ public int m_NumberToWin;                           // 获胜回合数。
        /*[ReadOnly]*/ public float m_StarTime;                        // 延迟0.5s后开始。
        /*[ReadOnly]*/ public float m_SuspendWait;                       //暂停结束后继续执行的时间
        /*[ReadOnly]*/ public float m_EndTime;                           // 延迟1s后进入下一个对局。



        public CameraManager m_CameraManager;                    // 在不同阶段的控制，请参考CameraControl脚本。
        public ScenesManager m_ScenesManager;
        public Text m_Text;                                    // 参考叠加文本显示获胜文本等。
        public GameObject m_TankPrefab;                        // 参考玩家将控制的预制物。
        public TankManager[] m_Tank;                           // 一组管理器，用于启用和禁用坦克的不同方面。

        public GameObject m_SpendPage;                        //暂停页面
        public GameObject m_SpendTime;                        //倒计时界面
        [HideInInspector] public bool Suspending;                              //暂停状态
        [HideInInspector] public bool IsEnding;                                //是否在结算界面



        private int m_RoundNUm = 0;                           // 记录回合数。
        private WaitForSeconds m_StartWait;                   // 开始时的延迟延迟。     WaitForSeconds     https://docs.unity.cn/cn/2019.4/ScriptReference/WaitForSeconds.html
        private WaitForSeconds m_EndWait;                     // 结束后的延迟。
        private TankManager m_RoundWinner;                    // 回合胜利者。
        private TankManager m_GameWinner;                     // 比赛胜利者。


        public GameObject m_EndPage;                          //对局结束界面
        public Text m_EndText;                               //对局最终信息


        [HideInInspector] public int m_SceneNumber;          //记录一下场景序号

        private void Start()
        {
            m_NumberToWin = ValueManager.NumberToWin;
            m_StarTime = ValueManager.StarTime;
            m_SuspendWait = ValueManager.SuspendWait;
            m_EndTime = ValueManager.EndTime;

            // 初始化延迟
            //API waitForSeconds   延迟执行协程
            //https://docs.unity.cn/cn/2019.4/ScriptReference/WaitForSeconds.html
            m_StartWait = new WaitForSeconds(m_StarTime);             
            m_EndWait = new WaitForSeconds(m_EndTime);


            //获取当前加载的场景序号
            m_SceneNumber = m_ScenesManager.SceneNumber();

            //实例化生成两个坦克
            AllTank();

            //相机的目标返回相机管理器
            SetCameraTargets();

            //初始化暂停状态
            Suspending = false;
            //float a = ValueManager.Instance.testNumber;
            //Debug.Log(ValueManager.Instance.testNumber);
            //Debug.Log(ValueManager.testNumber);


            // 游戏就开始了。(执行协程)
            StartCoroutine(GameLoop());
            


        }
        //当前仅用来做暂停判断
        private void Update()
        {
            //是否按下暂停键
            IsRoundSuspend();
            //SuspendEndbool();
        }

        //实例化生成两个坦克
        private void AllTank()
        {
            // 遍历所有坦克
            for (int i = 0; i < m_Tank.Length; i++)
            {
                // 创建它们，设置它们的玩家编号和控制所需的引用。
                m_Tank[i].m_Instance =                  //在生成点生成              API Instantiate  克隆     https://docs.unity.cn/cn/2019.4/ScriptReference/Object.Instantiate.html
                    Instantiate(m_TankPrefab, m_Tank[i].m_Bron.position, m_Tank[i].m_Bron.rotation);

                m_Tank[i].m_PlayerNumber = i + 1;
                m_Tank[i].Setup();  //坦克初始化
            }
        }

        //讲坦克的参数传递给跟踪的相机
        private void SetCameraTargets()
        {
            // 创建与坦克数量相同大小的转换集合。
            Transform[] targets = new Transform[m_Tank.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                //将其设置为适当的坦克变换。
                targets[i] = m_Tank[i].m_Instance.transform;   //将坦克实例的位置赋值给数组

            }
            // 这些是摄像机应该跟踪的目标。将坐标传入Camera中后续调用
            m_CameraManager.m_Targets = targets;     //相机跟踪数组元素

        }

        // 
        // 这将在游戏开始时调用，并将一个接一个地运行游戏的每个阶段。IEnumerator(协程)
        private IEnumerator GameLoop()
        {
            //游戏的初始化
            yield return StartCoroutine(RoundStarting());            // API StartCoroutine   https://docs.unity.cn/cn/2019.4/ScriptReference/MonoBehaviour.StartCoroutine.html

            //游戏运行中
            yield return StartCoroutine(RoundPlaying());

            // 回合结束的判定
            yield return StartCoroutine(RoundEnding());

            // 后续代码直到“RoundEnding”完成才会运行。 在这一点上，检查是否找到了游戏赢家。
            //有玩家则进入游戏结束阶段，
            if (m_GameWinner != null)
            {
                //TODO
                //弹出退出或重新开始窗口
                m_Text.text = string.Empty;
                m_EndText.text = EndMessage();
                m_EndPage.SetActive(true);
            }
            else
            {
                // 如果还没有赢家，重新启动这个协程，循环继续。
                //请注意，这个协程不会结束。这意味着当前版本的GameLoop将会结束。 Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
                StartCoroutine(GameLoop());
            }

        }


        private IEnumerator RoundStarting()
        {
            // 这个功能是用来打开所有的坦克和重置他们的位置和属性。
            ResetAllTanks();
            //禁止玩家操作和启用tank功能
            DisableTankControl();

            //重置摄像机位置，初始化
            m_CameraManager.ResectCamera();

            // 设定回合数，每次回合开始回合数++
            m_RoundNUm++;
            m_Text.text = "回合 " + m_RoundNUm;
            //Debug.Log("star");

            // 等待指定的时间长度，执行下一个协程
            yield return m_StartWait;
        }

        //游戏进行中
        private IEnumerator RoundPlaying()
        {

            // 游戏正式开始，让玩家控制坦克。
            EnableTankControl();
            //是否在回合结束计分状态 （false）   主要用于防止在计分状态暂停游戏出现bug
            IsEnding = false;   

            // 清除屏幕上的文本。
            m_Text.text = string.Empty;
            //Debug.Log("playing");
            // 直到没有坦克
            while (!IsOnlyOneTank())  //只剩一个的时候执行接下来的协程
            {
                // 下一帧返回。
                yield return null;
            }
        }

        //private IEnumerator RoundSuspend()
        //{
        //    yield return m_SuspendWait;
        //}

        private IEnumerator RoundEnding()
        {
            //防止结算中暂停   结算界面正在播放
            IsEnding = true;    
            // 阻止玩家对坦克的操作。
            DisableTankControl();

            //清除前一回合的赢家
            m_RoundWinner = null;

            // 现在回合结束了，看看是否有赢家。
            m_RoundWinner = GetRoundWinner();

            // 如果有赢家，增加他们的分数。
            if (m_RoundWinner != null)
                m_RoundWinner.m_WinTime++;

            //现在胜者的分数增加了，看看是否有人攒够积分
            m_GameWinner = GetGameWinner();

            // 显示回合信息
            string message = GameMessage();
            m_Text.text = message;
            // 等待指定的时间长度，直到将控制权交还给游戏循环。
            yield return m_EndWait;
        }


        // 判断是否只剩下一个坦克，是则返回true
        private bool IsOnlyOneTank()
        {
            // 从零开始计算剩下的坦克数量。
            int remainTank = 0;              //remain   剩余  

            // 
            for (int i = 0; i < m_Tank.Length; i++)
            {
                // 如果它们是活动的，则增加计数器。
                if (m_Tank[i].m_Instance.activeSelf)
                    remainTank++;
            }

            // 如果只剩一个或更少的坦克，则返回true，都活着则false。
            return remainTank <= 1;
        }


        // 获取赢家坦克
        // 
        private TankManager GetRoundWinner()
        {
            // 
            for (int i = 0; i < m_Tank.Length; i++)
            {
                //存在活着的坦克，则它是赢家。
                if (m_Tank[i].m_Instance.activeSelf)
                    return m_Tank[i];
            }

            // 如果没有赢家，返回null。
            return null;
        }


        // 是否有玩家赢得五次回合
        private TankManager GetGameWinner()
        {

            for (int i = 0; i < m_Tank.Length; i++)
            {
                // 如果其中一个有足够积分，返回。
                if (m_Tank[i].m_WinTime == m_NumberToWin)
                    return m_Tank[i];
            }

            // 大家都没有积分则返回空
            return null;
        }


        // 返回字符串消息，用于胜利结果显示
        private string GameMessage()
        {
            // 默认情况下，当一轮结束时没有赢家，所以默认结束消息是平局。  若有则被覆盖
                string message = "平局啦!";
                

            // 显示赢家信息    玩家编号 + 是本回合赢家
            
                message = "\n\n\n" + m_RoundWinner.m_ColoredPlayerText + " 是本回合赢家!\n太厉害啦!"+ "\n\n\n\n";
            for (int i = 0; i < m_Tank.Length; i++)
            {
                //通过所有的坦克，并将他们的分数添加到信息中。
                message += m_Tank[i].m_ColoredPlayerText + " 获得  ： " + m_Tank[i].m_WinTime + " 积分\n";   //（总分信息）
            }

            return message;

        }
        private string EndMessage()
        {
            string endmessage = m_GameWinner.m_ColoredPlayerText + " 获得了本局胜利!";  
            return endmessage;

        }


        // 这个功能是用来打开所有的坦克和重置他们的位置和属性。（每回合后初始化）
        private void ResetAllTanks()
        {
            for (int i = 0; i < m_Tank.Length; i++)
            {
                m_Tank[i].Reset();
            }
        }

        //允许玩家操作和启用tank功能
        private void EnableTankControl()
        {
            for (int i = 0; i < m_Tank.Length; i++)
            {
                m_Tank[i].EnableControl();
            }
        }

        //禁止玩家操作和启用tank功能
        private void DisableTankControl()
        {
            for (int i = 0; i < m_Tank.Length; i++)
            {
                m_Tank[i].DisableControl();
            }
        }

        //    private void test()
        //    {
        //        TankHealth testTankHealth = new TankHealth();
        //        testTankHealth.TankDamage(3f);
        //        testTankHealth.m_FullHealthColor = Color.green;

        //    }


        //控制游戏暂停
        private void IsRoundSuspend()
        {
            //esc被按下，游戏暂停
            if (Input.GetKeyUp(KeyCode.Escape)&& !IsEnding)
            {
                if (!Suspending)
                {
                    //暂停坦克控制
                    DisableTankControl();
                    //SuspendText.text = string.Empty;
                    m_SpendPage.SetActive(true);
                    Suspending = true;
                }
                else if (Suspending)  //暂停中,准备结束暂停
                {
                    m_SpendPage.SetActive(false);
                    m_SpendTime.SetActive(true);
                    DisableTankControl();

                    //延时三秒实现--恢复坦克运动
                    Invoke("SuspendEnd", 4f);
                }
            }
        }
        //结束暂停状态   （在 IsRoundSuspend 中延时三秒调用）
        private void SuspendEnd()
        {
            //恢复坦克运动
            EnableTankControl();
            //暂停状态改为false
            Suspending = false;
        }

        //是否在暂停中 （弃用，有BUG）
        public bool IsSuspend()
        {
            return Suspending;
        }

































    }

}






#endregion


#region           坦克管理器
using System;
using UnityEngine;



/*
 * 用来管理坦克的移动和行为
 * 与ganmemanager互动
 * 决定玩家在何时可以控制自己的坦克
 */
[Serializable]  //强制可序列化（使其在ui界面中显示）
public class TankManager
{


    public Color m_PlayerColor;                             // 玩家颜色。
    public Material m_PlayerMaterial;                       //玩家材质

    public Transform m_Bron;                                // 坦克出生的位置和方向。

    [HideInInspector] public int m_PlayerNumber;            // 用来指定玩家。
    [HideInInspector] public string m_ColoredPlayerText;    // 一串代表玩家的数字，颜色与他们的坦克相匹配。
    [HideInInspector] public GameObject m_Instance;         // 创建对象时对其实例的引用。
    [HideInInspector] public int m_WinTime;                 // 这个玩家到目前为止的胜利次数。


    private TankMovement m_Movement;                        // 参考坦克的移动脚本，用于禁用和启用控制。
    private TankFire m_Fire;                                // 参考坦克的射击脚本，用于禁用和启用控制。
    
    //初始化（位置，颜色等）
    public void Setup()
    {
        // 获取对组件的引用。
        m_Movement = m_Instance.GetComponent<TankMovement>();
        m_Fire = m_Instance.GetComponent<TankFire>();
        // 设置玩家号码，使其在脚本中保持一致。
        m_Movement.m_Playernum = m_PlayerNumber;
        m_Fire.m_Playernum = m_PlayerNumber;
        // 使用html富文本创建一个字符串
        //使得玩家代号颜色为玩家颜色
        m_ColoredPlayerText ="<color=#" + ColorUtility.ToHtmlStringRGB(m_PlayerColor) + ">玩家 " + m_PlayerNumber + "</color>";
        // 找到坦克的网格渲染组件。       renderers 为实例化后的tank的子对象的所有渲染网格
        MeshRenderer[] renderers = m_Instance.GetComponentsInChildren<MeshRenderer>();
        // 浏览所有的子模型网格Mesh并附上颜色
        for (int i = 0; i < renderers.Length; i++)
        {
            //将他们的材质颜色设置为该玩家特有的颜色。  
            //renderers[i].GetComponent<MeshRenderer>().sharedMaterial = nill;
            renderers[i].GetComponent<MeshRenderer>().sharedMaterial = m_PlayerMaterial;


            //TODO
            /*改变材质包
             * public Material m_material
             * Material m_material = new Material (shader .Find ("legcay Shaders /Transparent/diffuse"));
             * GetComponent <Renderer>().m_material = material;
             * 
             * 或
             * gameObject.GetComponent<Render>().material=新的材质,
             * 
             * 老款
             * Public Material myMaterial ; //定义材质类型变量,Public型，从外面拖拽上去//gameObject.renderer.material = myMaterial；
             */


        }
    }


    // 在游戏中玩家不应该有操作的阶段使用。
    public void DisableControl()
    {
        //禁用坦克移动 ， 坦克开火脚本
        m_Movement.enabled = false;          //enabled  启用
        m_Fire.enabled = false;

        //关闭屏幕显示（无意义）
        //m_CanvasGameObject.SetActive(false);
    }


    // 在游戏中玩家应该有操作的阶段使用。
    public void EnableControl()
    {
        m_Movement.enabled = true;        // enabled（启用）
        m_Fire.enabled = true;

        //m_CanvasGameObject.SetActive(true);
    }


    // 在每个回合的开始使用，使坦克进入默认状态。
    public void Reset()
    {
        //默认出生地点
        m_Instance.transform.position = m_Bron.position;
        m_Instance.transform.rotation = m_Bron.rotation;

        //不关闭无法重新激活初始化
        m_Instance.SetActive(false);         //https://docs.unity.cn/cn/2019.4/ScriptReference/GameObject.SetActive.html
        m_Instance.SetActive(true);
    }
}

#endregion


#region           坦克运动和控制类
       #region           坦克移动
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankMovement : MonoBehaviour
{
    //移动
    public int m_Playernum;           // 用来识别哪个坦克属于哪个玩家  
    /*[ReadOnly]*/public  float m_Speed;              // 坦克的速度配置 
    /*[ReadOnly]*/public  float m_TurnSpeed;        // 坦克的旋转速度 

    //声音
    public AudioSource m_MoveSound;          // 引擎音源 
    public AudioClip m_EngineQuiet;     //引擎静止
    public AudioClip m_EngineDriving;   //驾驶中
    public float m_PitchRange;       //引擎声音变化
    private float m_SonudStar;      //声音起始值

    //其他
    private string m_MoveAxis;      //移动键名称
    private string m_TurnAxis;      //转向键名称
    private Rigidbody m_Rigidbody;  //坦克刚体
    private float m_MoveValue;      //移动值
    private float m_TureValue;      //转向值

    /*private ParticleSystem[] m_ParticleSystem; */  //实例化粒子系统
    public GameObject SceneNumber;    //场景序号
    public GameObject m_Canvas;       //其他UI显示
    public GameObject m_TankLight;    //坦克车灯
    public GameObject m_TankTopLight; //坦克顶灯




   

    //每次使用坦克脚本时执行
    private void OnEnable()
    {
        m_Speed = ValueManager.TankSpeed;              // 坦克的速度配置 
        m_TurnSpeed = ValueManager.TurnSpeed;        // 坦克的旋转速度
        m_Rigidbody = GetComponent<Rigidbody>();
        //API isKinematic  控制是否受物理影响   如果启用了 isKinematic，则力、碰撞或关节将不再影响刚体。
        //https://docs.unity.cn/cn/2019.4/ScriptReference/Rigidbody-isKinematic.html
        m_Rigidbody.isKinematic = false;
        //Debug.Log("执行啦");
        //输入值为0
        m_MoveValue = 0f;
        m_TureValue = 0f;

        

    }

    private void OnDisable()
    {

        m_Rigidbody.isKinematic = true;


    }

    private void Start()
    {
        //记录按键操作，并且区分玩家，按键名称管理在 Edit/Project Setting/Input Manager
        m_MoveAxis = "Vertical" + m_Playernum;   //垂直
        m_TurnAxis = "Horizontal" + m_Playernum; //水平

        //存储原始音频大小，方便后期赋值
        m_SonudStar = m_MoveSound.pitch;
        //开启/关闭灯光
        TankLight();
    }

    private void Update()
    {
        //存取玩家输入键
        m_MoveValue = Input.GetAxis(m_MoveAxis);
        m_TureValue = Input.GetAxis(m_TurnAxis);

        //每次输入都将改变坦克运行状态，所以都要进行状态判定来确定播放的音频
        //播放引擎声音 TODO
        EnginePlay();

    }


    /*方法备注
     * 由于坦克音频运动只有两种可能，即运行或待机
     * 每当输入数值发生改变的时候，说明坦克的运动状态也发生改变
     * 可以不做判断直接将音频切换到另一个音频上
     * 并且在待机时由于输入都小于0.1，所以一直播放待机，但是输入值一旦增大，状态立马切换
     * 
     * 二号方案
     * 使当前位置和前几帧位置进行判断，有移动则是运行，无则是待机
     * 但是有延迟
     * 
     * 三号方案
     * 按下前进后退键则是运行
     * 松开则是待机
     */

    //根据坦克是否移动以及当前播放的音频播放正确的音频剪辑。（移动时播放引擎声音，停止时播放其他声音）
    private void EnginePlay()
    {
        //转向和移动接近停止时才需要切换待机和运行
        if (Mathf.Abs(m_MoveValue) < 0.1f && Mathf.Abs(m_TureValue) < 0.1f)
        {
            //待机状态
            if (m_MoveSound.clip == m_EngineDriving)
            {
                //状态改为空并将声音改为引擎待机声音，给个随机值，使得声音有变化
                m_MoveSound.clip = m_EngineQuiet;
                m_MoveSound.Play();
            }
        }
        else
        {
            if (m_MoveSound.clip == m_EngineQuiet)//引擎声音为引擎空转
            {
                m_MoveSound.clip = m_EngineDriving;
                m_MoveSound.Play();
            }
        }
    }

    //移动并旋转坦克   FixedUpdate用于物理计算的更新
    private void FixedUpdate()
    {
        Move();
        Turn();
    }


    //移动
    private void Move()
    {
        //移动的坐标值为   运动方向（前后方向or左右）*正负（前or后）*（速度*时间）即每秒运动speed距离
        Vector3 movemet = transform.forward * m_MoveValue * m_Speed * Time.deltaTime;
        m_Rigidbody.MovePosition(m_Rigidbody.position + movemet);//MovePosition   https://docs.unity.cn/cn/2019.4/ScriptReference/Rigidbody.MovePosition.html
    }
    //旋转
    private void Turn()
    {
        float turn = m_TureValue * m_TurnSpeed * Time.deltaTime;
        Quaternion turnRoation = Quaternion.Euler(0f, turn, 0f);//强制将角度转换为四元数，（角度不支持浮点类型）
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRoation);
    }

    //控制坦克灯光
    private void TankLight()
    {
        int ScenenNumber = SceneNumber.GetComponent<SceneNumber>().m_SceneNumber;
        //Debug.Log(ScenenNumber);
        if (ScenenNumber == 1)
        {
            m_TankLight.SetActive(false);
            m_TankTopLight.SetActive(false);
        }
        else if (ScenenNumber == 2)
        {
            m_TankLight.SetActive(true);
            m_TankTopLight.SetActive(true);
        }

    }
}


#endregion

       #region           坦克健康
       using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{

    private  float m_TankStarHealth;            //  每个坦克开始时的生命值。

    public Slider m_TankHealthSlider;              // 表示坦克当前生命值的滑块。
    public Image m_TankHealthFillImage;            // 滑动块的图像组件。  
    public Color m_FullHealthColor = Color.green;  // 当生命值满时，生命条的颜色。
    public Color m_ZeroHealthColor=Color.red;      // 当没有生命值时，生命条的颜色
    public GameObject m_TankDeathPrefab;           // 一个在Awake中实例化的预制件，当坦克b死亡时使用。


    private AudioSource m_TankDeathAudio;            // 当坦克爆炸时播放的音频源。
    private ParticleSystem m_TankDeathParticle;     // 坦克爆炸的粒子特效。 
    private float m_TankCurrentHealth;              // 坦克当前生命值。
    private bool m_TankIsDead;                      // 判断坦克是否死亡 


    private void Awake()
    {
        //实例化爆炸预制件，并在其上获得粒子系统的参考。 
        m_TankDeathParticle = Instantiate (m_TankDeathPrefab).GetComponent<ParticleSystem>();   //TODO

        //获取实例化预制件(爆炸的粒子特效)上的音频源的引用。 
        m_TankDeathAudio = m_TankDeathParticle.GetComponent<AudioSource>();

        //禁用预制件，这样它就可以在需要的时候被激活。 
        m_TankDeathParticle.gameObject.SetActive(false);
        //Debug.Log(m_TankStarHealth);
    }

    private void OnEnable()
    {
        // 当被启用时，重置坦克的健康状况以及死亡状态。
        m_TankStarHealth = ValueManager.TankStarHealth;
        m_TankCurrentHealth = m_TankStarHealth;
        m_TankIsDead = false;

        // 更新运行状况滑块的值和颜色。
        UpdateHealthUI();
    }

    //坦克受伤
    public void TankDamage(float amount)        //amount 即为收到的伤害值
    {

        // 根据造成的伤害减少当前生命值。
        m_TankCurrentHealth -= amount;

        // 更新运行状况滑块的值和颜色。
        UpdateHealthUI();
        
        // 如果当前运行状况为零或低于零且坦克还没死亡，则调用TankDeath方法。 
        if (m_TankCurrentHealth <= 0f && !m_TankIsDead)
        {
            TankDeath();
            
        }

    }


    private void TankDeath()
    {
        // 设置该标志，以便此函数只被调用一次。否则死亡方法一直播放
        m_TankIsDead = true;
        // 移动爆炸预制件到坦克的位置并生效。
        m_TankDeathParticle.transform.position = transform.position;
        m_TankDeathParticle.gameObject.SetActive(true);

        // 播放坦克爆炸的粒子特效和音频。
        m_TankDeathParticle.Play();
        m_TankDeathAudio.Play();    

        gameObject.SetActive(false);

        //测试用        ToDelete
        //调用其他类函数方法
        //重新开始
        //GameObject.Find("ScenesManager").GetComponent<ScenesManager>().Invoke("GameRestar", 1);  //Invoke 延时1s调用GameRestar（）方法
        //目标类挂载的对象名称              //类名           //方法名

    }

    // 更新运行状况滑块的值和颜色。
    private void UpdateHealthUI()
    {
        // 将当前声明的值赋值给UI滑块。
        m_TankHealthSlider.value = m_TankCurrentHealth;
        //根据当前血量和满血的百分比，在选定的颜色之间插入条的颜色。
        //Lerp线性插值  https://docs.unity.cn/cn/2019.4/ScriptReference/Color.Lerp.html
        m_TankHealthFillImage.color =
            Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_TankCurrentHealth / m_TankStarHealth);
    }

}


       #endregion

       #region 坦克开火
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//控制坦克的子弹和爆炸
public class TankFire : MonoBehaviour
{
    public int m_Playernum;                  // 用来识别不同的玩家。
    public Rigidbody m_Shell;                // 实例化预制体。
    public Transform m_FireTransform;        // 发射炮弹的坐标
    public Slider m_Aim;                     // 实例化滑块
    public AudioSource m_ShootAudio;         // 参考用于播放射击音频的音频源。注意:不同于运动音源。
    public AudioClip m_ChargingClip;         // 每次射击充能时播放的音频。
    public AudioClip m_FireClip;             // 每次射击时播放的音频。

    public Slider m_FireTimeSlider;              // 表示坦克冷却滑块。
    public Image m_FireTimeFillImage;            // 滑动块的图像组件。  
    public Color m_ZeroFireTime;                 //剩余CD为0的颜色
    public Color m_OneFireTime;                  //可以发射的CD的颜色

    public  float m_MinFire;              // 初始给予炮弹的力。
    public float m_MaxFire;              // 在最大充能时间内按动射击按钮给予炮弹的力。
    public float m_MaxFireTime;     // 炮弹最大充能所需时间。
    public bool m_ChargingClipIsPlaying;                       //充能声音是否在播放

    private float m_ReFireTime = ValueManager.ReFireTime;        //重新发射的冷却时间
    private string m_FireButtonName;                            // 长按发射的输入键（即空格）。
    private float m_UpFireButtonValue;                          // 当发射按钮被释放时，将给予炮弹的力量。
    //private bool m_UpFireButtonValueEnabled;                  // 判断Fire是否松开
    private float m_ChargeSpeed;            // 根据最大充电时间，发射力增加的速度。
    private bool m_FireShoot;               // 是否已经发射。    
    private float m_FireTime;               //上次发射过了多久


    private void OnEnable()
    {
        m_MinFire = ValueManager.MinFire;
        m_MaxFire = ValueManager.MaxFire;
        m_MaxFireTime = ValueManager.MaxFireTime;
        m_ReFireTime = ValueManager.ReFireTime;

        //当坦克启动时，重置发射力和UI 发射状态和发射冷却CD
        m_UpFireButtonValue = m_MinFire;
        m_Aim.value = m_MinFire;
        m_FireTimeSlider.value = m_ReFireTime;
        m_FireShoot = false;
        m_FireTime = m_ReFireTime;
        m_ChargingClipIsPlaying = false;


    }


    private void Start()
    {
        //记录不同玩家输入
        m_FireButtonName = "Fire" + m_Playernum;

        // 发射力充能的速率是在最大充电时间内可能产生的力的范围。
        m_ChargeSpeed = (m_MaxFire - m_MinFire) / m_MaxFireTime;//（最大值-最小值）/最小到最大的时间 =充能速率
    }

    private void Update()
    {
        // 滑块应该有最小发射力的默认值。
        m_Aim.value = m_MinFire;

        //距离上次发射小于发射CD时，距离上次发射时间值叠加
        if(m_FireTime < m_ReFireTime)
        {
            m_FireTime += Time.deltaTime;
        }
        else  
        {
            m_FireTime = m_ReFireTime;
            //Debug.Log("Fire");
        }
        //更新炮弹CD时间
        UpdateFireTime();
        //控制蓄力声音中断
        EndPlaym_ChargingClip();



        // 开火按钮刚刚开始被按下
        if (Input.GetButtonDown(m_FireButtonName))
        {
            //重置发射状态和发射力量。
            m_FireShoot = false;
            m_UpFireButtonValue = m_MinFire;
            
        }
        //按住了射击键，而炮弹还没有发射
        else if (Input.GetButton(m_FireButtonName) && !m_FireShoot)
        {
            // 增加发射力并更新滑块。
            if (m_UpFireButtonValue<=m_MaxFire)
            {
                m_UpFireButtonValue += m_ChargeSpeed * Time.deltaTime;
                if (m_UpFireButtonValue >= 25f && !m_ChargingClipIsPlaying)
                {
                    // 播放充能声音
                    m_ShootAudio.clip = m_ChargingClip;
                    m_ShootAudio.Play();
                    m_ChargingClipIsPlaying = true;
                }
            }
            else
            {
                m_UpFireButtonValue = m_MaxFire;
            }
            m_Aim.value = m_UpFireButtonValue;
        }
        // 发射按钮被释放，炮弹还没有发射,使其发射
        else if (Input.GetButtonUp(m_FireButtonName) && !m_FireShoot)
        {
            m_ChargingClipIsPlaying = false;
            if (m_FireTime == m_ReFireTime)
            {
                Fire();
            }
        }
        // 如果超过了最大力，而炮弹还没有发射,且松开按键
        else if (m_UpFireButtonValue >= 
            m_MaxFire && !m_FireShoot && !Input.GetButtonUp(m_FireButtonName))
        {
            
            m_UpFireButtonValue = m_MaxFire;
            //距离上次发射时间==CD，可以发射
            if (m_FireTime == m_ReFireTime)
            {
                Fire();
            }
        }

    }

    private void Fire()
    {
        // 使得状态改为发射了。
        m_FireShoot = true;

          
        // 创建一个子弹的实例，并将炮口的位置和旋转赋值给新创建的实例，并引用它的刚体。
        Rigidbody ShellInstance =
            Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation);

        // 设置炮弹的速度方向为坦克的前进方向。
        //velocity https://docs.unity.cn/cn/2019.4/ScriptReference/Rigidbody-velocity.html
        //  forward   https://docs.unity.cn/cn/2019.4/ScriptReference/Vector3-forward.html   
        ShellInstance.velocity = m_UpFireButtonValue * m_FireTransform.forward;
        //                              发射速度           起始坐标.向前坐标

        // 改变剪辑射击剪辑并播放它。
        m_ShootAudio.clip = m_FireClip;
        m_ShootAudio.Play();

        //距离上次发射时间清0
        m_FireTime = 0;

        // 重置发射按钮。这是一种预防措施，以防丢失按钮事件。
        m_UpFireButtonValue = m_MinFire;
    }

    //控制蓄力声音中断
    public void EndPlaym_ChargingClip()
    {
        //如果抬起开火键且蓄力声音正在播放
        if (Input.GetButtonUp(m_FireButtonName)&& m_ChargingClipIsPlaying)
        {
            m_ShootAudio.clip = null;  //声音为空
        }
        
    }

    //控制坦克的发射cd的UI显示条
    public void UpdateFireTime()
    {

        m_FireTimeSlider.value = m_FireTime;

        m_FireTimeFillImage.color =
            Color.Lerp(m_ZeroFireTime, m_OneFireTime, m_FireTime / m_ReFireTime);

    }
}

#endregion

#endregion


#region     UI和场景管理
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    [HideInInspector] public GameObject MapChange;              //地图选择
    [HideInInspector] public GameObject Spendpage;              //暂停界面
    

    //[System.Obsolete]

    private void Start()
    {
        //test();
    }

    public int SceneNumber()
    {
        //判断一下当前所在场景（day or night）
        Scene a = SceneManager.GetActiveScene();
        // API  bulidIndex                          https://docs.unity.cn/cn/2019.4/ScriptReference/SceneManagement.Scene-buildIndex.html
        int m_SceneNumber = a.buildIndex;
        return m_SceneNumber;
    }


    public void GameStar()
    {        
        MapChange.SetActive(true);
    }

    public void GameExit()
    {
        Application.Quit();
    }

    public void DayMap()
    {
        SceneManager.LoadScene(1);
    }

    public void NightMap()
    {
        SceneManager.LoadScene(2);
    }

    public void Close()
    {
        MapChange.SetActive(false);
    }

    //重新开始游戏
    public  void GameRestar()
    {
        SceneManager.LoadScene(SceneNumber());
    }
    
    //public void SuspendIsFalse()
    //{
    //    Spendpage.SetActive(false);
    //    Debug.Log("页面已关闭");
    //}
    //显示
    public virtual void Show()  //virtual声明成一个虚方法
    {
        gameObject.SetActive(true);
    }
    //隐藏
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    //private void test()
    //{
         
    //    Debug.Log (SceneNumber());  
    //}
    public void ReMenu()
    {
        SceneManager.LoadScene(0);
    }
}

#endregion


#region    炮弹爆炸管理
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellExplosion : MonoBehaviour
{
    public LayerMask m_TankMask;                             // 用来区分普通碰撞物体和坦克，当前为坦克  LayerMask图层，类似与ps中用于分组
    public ParticleSystem m_ShellExplosionParticles;        // 子弹爆炸效果的粒子动画。     
    public AudioSource m_ShellExplosionSound;               // 子弹爆炸时播放的音频。

    /*public Spendpage spendpage;*/                            //暂停脚本


    public float m_MaxDamage=100f;                          // 子弹最大伤害。
    public float m_Shock=1000f;                             // 爆炸范围的中心的坦克所承受的冲击波推力。
    public float m_ShellDisappearTime = 2f;                 // 子弹碰撞后消失的时间，2s。
    public float m_ShellExplosionRadius = 10f;             //  子弹爆炸的球体半径



    private void Start()
    {
        // 如果到那时还没有被摧毁，那就在它的寿命结束后摧毁。
        Destroy(gameObject, m_ShellDisappearTime);    //https://docs.unity.cn/cn/2019.4/ScriptReference/Object.Destroy.html
    }

    //碰撞事件一旦发生，则调用OnTriggerEnter方法，比FixedUpdate执行次数小  (如果另一个碰撞器进入到子弹触发器中，则调用)
    private void OnTriggerEnter(Collider other)       // Collider  所有碰撞体的基类
    {
        // 收集子弹碰撞范围内的所有碰撞体，生成数组     OverlapSphere   https://docs.unity.cn/cn/2019.4/ScriptReference/Physics.OverlapSphere.html
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ShellExplosionRadius, m_TankMask);
                                                   // 爆炸中心              爆炸半径                在那些层查询(plays)

        for(int i = 0; i < colliders.Length; i++)
        {
            // 获取他们的刚体。
            Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
            // 如果没有刚体，继续下一个物体。
            if (!targetRigidbody)

                continue;

            // 添加爆炸的推力。 API addExplosionForce  向模拟爆炸效果的刚体施加力
            // https://docs.unity.cn/cn/2019.4/ScriptReference/Rigidbody.AddExplosionForce.html
            targetRigidbody.AddExplosionForce(m_MaxDamage*5, transform.position, m_ShellExplosionRadius);
                                            //力量float（中心伤害）         爆炸中心                 范围

            // 找到与刚体相关的TankHealth脚本。
            TankHealth TankHealth = targetRigidbody.GetComponent<TankHealth>();

            // 根据目标与炮弹的距离计算出目标应该承受的伤害。
            float amount = MakeDamage(targetRigidbody.position);

            //将伤害给坦克
            TankHealth.TankDamage(amount);


            //判断是否暂停   TODO  BUG
            //Spendpage spendpage = new Spendpage();
            //if (spendpage.IsSuspending())
            //{
            //    amount = 0;
            //}
            //else
            //{

            //}


        }
        // 使爆炸粒子动画没有父物体（直接删除会使得下次无法引用）
        m_ShellExplosionParticles.transform.parent = null;
        // 播放粒子系统。
        m_ShellExplosionParticles.Play();
        // 播放爆炸音效。
        m_ShellExplosionSound.Play();

        // 一旦粒子动画播放完成，摧毁它们所在的游戏对象
        ParticleSystem.MainModule mainModule = m_ShellExplosionParticles.main;
        //粒子系统 MainModule 的脚本接口。 https://docs.unity.cn/cn/2019.4/ScriptReference/ParticleSystem.MainModule.html
        Destroy(m_ShellExplosionParticles.gameObject, mainModule.duration);

        Destroy(gameObject);    

    }

    private float MakeDamage (Vector3 targetPosition)
    {
        //定义一个坐标为 目标坐标-子弹爆炸点坐标           即目标和子弹爆炸点的差
        Vector3 ToTarget = targetPosition  - transform.position;

        // 计算炮弹到目标的距离。（即向量长度）爆炸距离
        float ExplosionDistance = ToTarget.magnitude;     // magnitude 向量长度

        // 计算目标和子弹爆炸中心相距(爆炸半径)的比例。
        float i = (m_ShellExplosionRadius - ExplosionDistance) / m_ShellExplosionRadius;

        // 根据最大可能伤害的比例计算伤害。
        float damage = i * m_MaxDamage;

        // 确保最小伤害总是0。
        damage = Mathf.Max(0f, damage);

        return damage;  
    }
}

#endregion


#region    相机管理
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public float m_DampTime = 0.2f;     //相机移动并不是立刻的，延迟0.2s后进行移动  
    public float m_Blank = 4f;          // 顶部/底部最目标和屏幕边缘之间的空间。  （屏幕的留白，保证坦克不超出屏幕边缘）
    public float m_MinSize = 7f;        // 防止放的过大，坦克贴近时最小显示范围仍有6.5f       

    private Camera m_Camera;                   // 标记相机 
    private float m_ZoomSpeed;                 // 用来平滑前后（正交）移动 
    private Vector3 m_CameraMove;              // 用来平滑左右移动  
    private Vector3 m_CameraTargetPosition;    // 相机目标到达的地点 （预期的位置）

    [HideInInspector] public Transform[] m_Targets;             // 摄像机要瞄准的所有目标。（即两个坦克）


    private void Awake()
    {
        m_Camera = GetComponentInChildren<Camera>();
    }

    private void FixedUpdate()
    {
        Move();//将相机移动到需要的位置。
        Zoom();//将相机进行合适的缩放。
    }

    private void Move()
    {
        //获取两个玩家的中心点
        FindAveragePosition();
        //移动到目标位置
        //当前位置  目标到达点  参考变量（ref表示将要回到那个变量） 所用时间 Vector3.SmoothDamp  https://docs.unity.cn/cn/2019.4/ScriptReference/Vector3.SmoothDamp.html
        transform.position = Vector3.SmoothDamp(transform.position, m_CameraTargetPosition, ref m_CameraMove, m_DampTime);
        //                                              当前位置               目标位置          平滑移动 (float)   多少时间完成
    }

    //获取两个玩家的中心点
    private void FindAveragePosition()
    {
        //定义一个平均坐标
        Vector3 averagePos = new Vector3();
        //当前玩家数量为0
        int PlayNumber = 0;
        //遍历每个玩家 
        for (int i = 0; i < m_Targets.Length; i++)
        {
            //将每个玩家的坐标值相加
            averagePos += m_Targets[i].position;
            //每遍历一个，玩家数量加1
            PlayNumber++;

        }
        //如果厂上玩家数大于0
        if (PlayNumber > 0)
        {
            //坐标和÷玩家数量
            averagePos /= PlayNumber;
            //赋值给期望移动的坐标
            m_CameraTargetPosition = averagePos;
        }
    }

    //将相机进行合适的缩放。
    private void Zoom()
    {

        // 根据所需的位置找到所需的大小，并平稳地过渡到该大小。 
        float TargetsSize = FindRequiredSize();
        
        //orthographicSize(正切角度（投影）大小)API https://docs.unity.cn/cn/2019.4/ScriptReference/Camera-orthographicSize.html
        //SmoothDamp随时间推移将一个值逐渐改变为所需目标。  https://docs.unity.cn/cn/2019.4/ScriptReference/Mathf.SmoothDamp.html
        m_Camera.orthographicSize = Mathf.SmoothDamp(m_Camera.orthographicSize, TargetsSize, ref m_ZoomSpeed, m_DampTime);
    }                                                    //当前大小               目标大小          平滑缩放       多少时间完成

    //获得一个相机的期望大小并返回（size）  
    private float FindRequiredSize()
    {
        //找到相机在空间中的目标移动位置            InverseTransformPoint  将 position 从世界空间变换到本地空间。/*transform.InverseTransformPoint(m_CameraTargetPosition);*/
        Vector3 m_TargetPosition = transform.InverseTransformPoint(m_CameraTargetPosition);
        //从0开始相机大小的计算
        float size = 0;
        //遍历所有玩家
        for (int i = 0; i < m_Targets.Length; i++)
        {
            // 在相机的局部空间中查找目标的位置
            Vector3 playerPos = transform.InverseTransformPoint(m_Targets[i].position);
            // 从相机的局部空间的期望位置到目标的位置的差。
            Vector3 desiredPosToTarget = playerPos - m_TargetPosition;
            // 从当前的尺寸中选择最大的和坦克“向上”或“向下”距离相机。
            size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));
            // 从当前的尺寸和计算的尺寸中选择最大的，基于坦克是在相机的左边还是右边。
            size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / m_Camera.aspect);   //m_Camera.aspect   宽高比（宽度除以高度）
        }
        //给出边缘的空白区域
        size += m_Blank;
        //相机最近不会小于的尺寸
        size = Mathf.Max(size, m_MinSize);
        return size;
    }

    //游戏重新开始后重置摄像机位置
    public void ResectCamera()
    {
        //获取两辆车的平均中心点
        FindAveragePosition();

        // 在没有阻尼的情况下，将相机的位置设置为所需的位置。
        transform.position = m_CameraTargetPosition;

        // 找到并设置所需的相机的正切大小。
        m_Camera.orthographicSize = FindRequiredSize(); 
    }
}
   



#endregion


#region    昼夜交替
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public GameObject Sun;         //太阳
    public GameObject SceneNumber;

    private bool SunLight;    //是否由太阳光
    public float m_SunMoveSpeed; //太阳移动速度
    [HideInInspector]private float SunValue;
    void Start()
    {
        //实例化太阳灯
        m_SunMoveSpeed = ValueManager.SunMoveSpeed;
        SunLight = false;
    }

    private void Update()
    {
        SunUp();
    }
    void SunUp()
    {  //获取光照强度组件
        Sun.GetComponent<Light>().intensity = SunValue;

        if (SunValue < 1 && ! SunLight)
        {   //更改光照值并附加当前光照状态
            SunValue += Time.deltaTime * m_SunMoveSpeed;  
            if (SunValue >=1)
            {
                SunLight = true;
            }
        }    
        if(SunLight)
        {
            SunValue -= Time.deltaTime * m_SunMoveSpeed;  
            if (SunValue <= 0)
            {
                SunLight = false;
            }  
        } 
     
    }
}

#endregion


#region   面板数据只读模式
using UnityEditor;

namespace UnityEngine
{
    /// <summary>
    /// 设置属性只读
    /// </summary>
    public class ReadOnly : PropertyAttribute
    {

    }
    [CustomPropertyDrawer(typeof(ReadOnly))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endregion


#region 模型贴图更改
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankSkin : MonoBehaviour
{

    public Color m_PlayerColor;                             // 这是这个坦克将要生成的颜色。
    [HideInInspector] public GameObject m_Instance;         // 创建对象时对其实例的引用。

    //游戏对象
    public GameObject Tank1;
    //材质
    public Material green;
    public Material blue;
    public Material red;
   

    public float num;
    void test1()
    {

        Tank1.GetComponent<MeshRenderer>().sharedMaterial = red;
    }
    void Update()
    {
        num += Time.deltaTime;
        if (num > 2)
        {
            Setup();
            num = 0;
        }

    }
    public void Setup()
    {
        // 找到坦克的网格渲染组件。       renderers 为实例化后的tank的子对象的所有渲染网格
        MeshRenderer[] renderers = Tank1.GetComponentsInChildren<MeshRenderer>();
        // 浏览所有的子模型网格Mesh并附上颜色
        for (int i = 0; i < renderers.Length; i++)
        {
            //将他们的材质颜色设置为该玩家特有的颜色。  
            renderers[i].GetComponent<MeshRenderer>().sharedMaterial = blue;

            //TODO
            /*改变材质包
             * public Material m_material
             * Material m_material = new Material (shader .Find ("legcay Shaders /Transparent/diffuse"));
             * GetComponent <Renderer>().m_material = material;
             * 
             * 或
             * gameObject.GetComponent<Render>().material=新的材质,
             * 
             * 老款
             * Public Material myMaterial ; //定义材质类型变量,Public型，从外面拖拽上去//gameObject.renderer.material = myMaterial；
             */
        }
    }
}
#endregion


#region 倒计时协程
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//  控制暂停倒计时的显示         TODO  用协程方法重构一次
public class SpendTime : ScenesManager
{
    [HideInInspector] public Text NumberText;

    [HideInInspector] public int Number;
    [HideInInspector] private int m_FirstNumber = ValueManager.FirstNumber;           //暂停倒计时从几开始
    [HideInInspector] private WaitForSeconds WaitTime;


    //使用协程重构
    void OnEnable()
    {
        Number = m_FirstNumber + 1;
        NumberText.text = string.Empty;
        WaitTime = new WaitForSeconds(1f);
        StartCoroutine(CountDown());
        //Debug.Log("脚本启动");
    }
    IEnumerator CountDown()
    {
        yield return StartCoroutine(CountDown1());
        if (!(Number == 0))
        {
            StartCoroutine(CountDown());
        }
        else
        {
            NumberText.text = string.Empty;
            Number = m_FirstNumber;
            Hide();
        }
    }

    IEnumerator CountDown1()
    {
        Number--;
        if (Number == 0)
        {
            NumberText.text = "开始！";
        }
        else
        {
            NumberText.text = Number.ToString();
        }
        yield return WaitTime;
    }




    //[HideInInspector] private float time;
    //[HideInInspector] public int Number;
    //[HideInInspector] private int FirstNumber = 4;

    //void Awake()
    //{
    //    time = 1;
    //    Number = FirstNumber;
    //    NumberText.text = string.Empty;
    //    Debug.Log("唤醒啦");
    //}
    //void Update()
    //{
    //    if (Number > 0)
    //    {
    //        if (time < 1)
    //        {
    //            time += Time.deltaTime;
    //        }
    //        else if (time >= 1)
    //        {
    //            Number--;
    //            NumberText.text = Number.ToString();
    //            time = 0;
    //        }
    //    }
    //    if (Number == 0)
    //    {

    //        time = 1;
    //        NumberText.text = string.Empty;
    //        Number = FirstNumber;
    //        Hide();

    //    }
    //}
}

#endregion


#region    单例数值和方法管理
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValueManager : MonoBehaviour
{
    
    public static ValueManager instance;

    #region    游戏管理数据
    public static int NumberToWin = 3;                           // 获胜回合数。
    public static float StarTime = 0.5f;                        // 延迟0.5s后开始。
    public static float SuspendWait = 1f;                       //暂停结束后继续执行的时间
    public static float EndTime = 1f;                           // 延迟1s后进入下一个对局。
    public static int FirstNumber = 3;                          //暂停倒计时从几开始
    #endregion

    #region       坦克数值类

    //坦克生命值类
    public static float TankStarHealth = 100f;      //  每个坦克开始时的生命值。

    //坦克移动类
    public static float TankSpeed = 10f;         // 坦克的速度配置 
    public static float TurnSpeed = 200f;        // 坦克的旋转速度 

    //炮弹伤害类
    public static float MinFire = 20f;          // 初始给予炮弹的力。
    public static float MaxFire = 40f;          // 在最大充能时间内按动射击按钮给予炮弹的力。
    public static float MaxFireTime = 1f;       // 炮弹最大充能所需时间。
    public static float ReFireTime = 2f;        //重新发射的冷却时间

    #endregion


    #region       黑夜模式太阳移动速度
    public static float SunMoveSpeed = 0.05f;                   //太阳移动速度
    #endregion

    


    //方法引用
    public static ValueManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ValueManager>();
            }
            //若还为空则直接生成一个对象并附加脚本
            if (instance == null)
            {
                GameObject ValueManger = new GameObject();
                ValueManger.AddComponent<ValueManager>();
                instance = ValueManger.AddComponent<ValueManager>();
            }
            return instance;
        }

    }

    //游戏运行前执行
    private void Reset()
    {
        
    }

}

#endregion


#region   相对旋转
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_NoRotate : MonoBehaviour
{
    // 确保世界空间UI生命值条等元素朝向正确的方向
    //m_localRotation 相对于父级变换旋转的变换旋转
    private Quaternion m_localRotation;               //定义一个四元数，用来相对旋转   现场开始时的局部旋转。

    private void Start()
    {
        //令相对角度四元数等于最初的模型和血条的相对值
        m_localRotation = transform.localRotation;   //localRotation 用于相对旋转，限于四元数  https://docs.unity.cn/cn/2019.4/ScriptReference/Transform-localRotation.html

    }

    private void Update()
    {
        //不断使得相对旋转角度相同，即角度不变
        transform.rotation = m_localRotation;  //rotation 旋转属性
    }
}

#endregion


#region 

#endregion





