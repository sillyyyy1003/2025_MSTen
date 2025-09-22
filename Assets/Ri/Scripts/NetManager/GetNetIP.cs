using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class LocalIPAddress : MonoBehaviour
{
    void Start()
    {
        string localIP = GetLocalIPv4();
        Debug.Log("当前局域网 IPv4 地址: " + localIP);
    }

    /// <summary>
    /// 获取本机的局域网 IPv4 地址
    /// </summary>
    string GetLocalIPv4()
    {
        string localIP = "";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            // 只要 IPv4，且不是回环地址
            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
            {
                localIP = ip.ToString();
                break;
            }
        }
        return string.IsNullOrEmpty(localIP) ? "未找到局域网 IPv4 地址" : localIP;
    }
}