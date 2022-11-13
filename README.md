# Rust.Oxide.ItemChsName
A plugin to get rust item's Chinese name and with online update

**Item Chs Name API** is a plugin to get rust item's Chinese name and with online update.

It syncs data from a CDN url of [XiuZheZhiJia - SaaS   H5 rust server shop solution](https://rust.phellytech.com) which support for web shop with WeChatPay and Alipay for Chinese players and owners.

![XiuZheZhiJia - SaaS   H5 rust server shop solution](https://rust.phellytech.com/img/txtLogoZipped.png)

## Features
* Show Item's Chinese Name
  1. Use Console cmd.
  2. Use Chat cmd.

## Permissions
None - Every player can use cmd.

## Console Commands
* `itemchsname <itemcode>` - Show an item's Chinese name with its item code.

## Chat Commands
* `/itemchsname <itemcode>` - Show an item's Chinese name with its item code.

## Default Configuration
```
{
  "UpdateDateTime": ""
}
```

## StoredData
```
{
  "ItemChsName List": "{...}"//Contains all 780  items
}
```

## Default Translation
en:
```
{
  "Chat Resp": "ItemCode: {0}, ItemChsName: {1}",
  "Get Item Chs Name Failed": "# Get Failed #"
}
```

zh-CN:
```
{
  "Chat Resp": "道具代码：{0}，道具中文名：{1}",
  "Get Item Chs Name Failed": "# 获取失败 #"
}
```
