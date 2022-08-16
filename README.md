# IntellisenseTranslator



## 如何更新字典并翻译？
1. 使用管理员身份运行 Intellisense汉化工具.exe
2. 输入需要翻译的文件夹路径
3. [提示:是否更新字典文件?(y/n)] 输入 Y/N
4. 如果选择更新字典，更新完成后会自动创建一个[yyyyMMddHHmmssfff].json的字典文件，如果你愿意分享，请提交你的文件。
5. [提示:使用字典翻译xml文件?(Y/N)] 输入 Y
6. 翻译完成后，在软件所在文件夹会自动创建一个translate文件夹，复制里面的所有文件，并替换到相应的文件夹覆盖。
7. 重启Visual Studio。







## 当出现未翻译的内容如何处理？
![image](https://user-images.githubusercontent.com/13758552/184807374-ad6a54d3-e1f8-4013-b0b6-0b1e661a7f88.png)
可以右键转到定义，并将滚动条拉到文件最上方，复制所引用的dll文件夹路径：
![image](https://user-images.githubusercontent.com/13758552/184807607-d1f48153-86a2-4e89-a905-9c5870ea07a7.png)
打开这个文件夹路径后，可以看到xml的文件：
![image](https://user-images.githubusercontent.com/13758552/184807756-7c5982a5-4119-4c71-b4d8-69f1db080f97.png)

此时你可以通过翻译工具，对这个目录进行翻译，并将翻译后的xml文件，复制到此目录替换。
你也可以直接跳转到packages文件夹，对所有nuget缓存的xml文件进行翻译：

![image](https://user-images.githubusercontent.com/13758552/184807980-d0b6f696-b508-4d73-b87a-0db47124f036.png)
