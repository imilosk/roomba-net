# OpenWrt network setup

This page describes a router setup that keeps the robots fully local:

- The robots are on their own IoT network and cannot reach the internet.
- Your main network can still reach the robots, so RoombaNet works.
- The robots get correct time from the router, even with no internet access.

It was tested on OpenWrt 25.12 with an i3+ and a Braava jet m6. The addresses below match that
setup. Change them to fit your network.

| Item | Value |
|---|---|
| Main network (`lan`) | `192.168.1.0/24` |
| IoT network (`iot`) | `192.168.20.0/24`, router at `192.168.20.1`, bridge `br-iot` |
| Robots | fixed DHCP leases, for example `192.168.20.65` and `192.168.20.66` |

## Why time matters

The robots set their clock with NTP. Without a time source, the clock drifts or starts at a
wrong value after a reboot. A wrong clock breaks:

- the weekly schedule, which runs on the robot's clock,
- mission timestamps and history,
- time values in the status, such as the recharge and resume times.

The robots do not use the NTP server that DHCP offers (option 42). They send NTP requests to
their own built-in servers, which are on the internet and are blocked here. The fix is a DNAT
rule that sends every NTP request from the robots to the router, whatever server the robot asked
for. The router answers with correct time.

## 1. IoT network and firewall zone

`/etc/config/network`:

```
config device
	option type 'bridge'
	option name 'br-iot'

config interface 'iot'
	option device 'br-iot'
	option proto 'static'
	option ipaddr '192.168.20.1'
	option netmask '255.255.255.0'
```

Create a 2.4 GHz Wi-Fi network for the robots and attach it to the `iot` interface
(`option network 'iot'` in `/etc/config/wireless`).

`/etc/config/firewall`:

```
config zone
	option name 'iot'
	list network 'iot'
	option input 'REJECT'
	option output 'ACCEPT'
	option forward 'REJECT'

# The main network can reach the robots (MQTT on 8883, discovery on 5678).
config forwarding
	option src 'lan'
	option dest 'iot'
```

There is no `iot` to `wan` forwarding, so the IoT network has no internet access.

The router itself only accepts the services the robots need: DHCP, DNS, and NTP.

```
config rule
	option name 'Allow-IoT-DHCP'
	option src 'iot'
	option proto 'udp'
	option dest_port '67'
	option target 'ACCEPT'

config rule
	option name 'Allow-IoT-DNS'
	option src 'iot'
	option proto 'tcp udp'
	option dest_port '53'
	option target 'ACCEPT'

config rule
	option name 'Allow-IoT-NTP'
	option src 'iot'
	option proto 'udp'
	option dest_port '123'
	option target 'ACCEPT'
```

## 2. Fixed addresses for the robots

RoombaNet connects to the robots by IP, so give them fixed leases. `/etc/config/dhcp`:

```
config host
	option name 'roomba'
	option mac '<roomba-mac>'
	option ip '192.168.20.65'

config host
	option name 'braava'
	option mac '<braava-mac>'
	option ip '192.168.20.66'
```

Discovery broadcast does not cross from `lan` to `iot`. Use
`discover --target 192.168.20.0/24`, or the `target` parameter of the API.

## 3. Block the iRobot cloud

The zone setup already blocks internet access. These two rules make sure the robots stay
blocked even if someone adds an `iot` to `wan` forwarding later.

Reject all forwarded traffic from the robots:

```
config rule
	option name 'Block iRobot outbound (IoT src)'
	option src 'iot'
	option dest '*'
	list src_mac '<roomba-mac>'
	list src_mac '<braava-mac>'
	option proto 'all'
	option target 'REJECT'
```

Answer DNS lookups for the iRobot API with `0.0.0.0`. In `/etc/config/dhcp`, in the
`dnsmasq` section:

```
	list address '/irobotapi.com/0.0.0.0'
```

## 4. Time (NTP)

### Run an NTP server on the router

The router syncs its own clock from the internet and serves time on the IoT network.
`/etc/config/system`:

```
config timeserver 'ntp'
	option enabled '1'
	option enable_server '1'
	option interface 'iot'
	list server '0.openwrt.pool.ntp.org'
	list server '1.openwrt.pool.ntp.org'
	list server '2.openwrt.pool.ntp.org'
	list server '3.openwrt.pool.ntp.org'
```

Also offer the router as the NTP server over DHCP, for devices that use it. In
`/etc/config/dhcp`:

```
config dhcp 'iot'
	option interface 'iot'
	list dhcp_option '42,192.168.20.1'
```

### Redirect the robots' NTP requests to the router

The robots ignore option 42, so redirect their NTP traffic. `/etc/config/firewall`:

```
config redirect 'roomba_ntp_redirect'
	option name 'Roomba-NTP-to-router'
	option src 'iot'
	list src_mac '<roomba-mac>'
	list src_mac '<braava-mac>'
	option proto 'udp'
	option src_dport '123'
	option dest_ip '192.168.20.1'
	option dest_port '123'
	option target 'DNAT'
```

How it works:

1. The robot sends an NTP request to its built-in server on the internet.
2. The DNAT rule changes the destination to `192.168.20.1:123`, before any forwarding decision.
3. The packet is now addressed to the router, so the `Allow-IoT-NTP` input rule accepts it. The
   block rule from step 3 does not apply, because the packet is no longer forwarded.
4. The router's NTP server replies. The reply goes back through the same connection, so the
   robot sees an answer from the server it asked.

Apply the changes:

```bash
service firewall restart
service sysntpd restart
service dnsmasq restart
```

### Check that it works

The DNAT rule and the input rule have packet counters. They go up when the robots ask for time:

```bash
nft list chain inet fw4 dstnat_iot
nft list chain inet fw4 input_iot | grep 123
```

Example:

```
udp dport 123 ether saddr { ... } counter packets 6 bytes 456 dnat ip to 192.168.20.1:123 comment "!fw4: Roomba-NTP-to-router"
```

The counter for the block rule should stay at `0`:

```bash
nft list chain inet fw4 forward_iot | grep iRobot
```

The robots ask for time regularly, so the counter goes up on its own.

### Notes

- The redirect is IPv4 only. NTP over IPv6 to the internet is still blocked by the block rule.
- The router must have correct time itself. With no internet on the router, its NTP server has
  no source.
- Add the MAC address of each new robot to the redirect rule and to the block rule.
