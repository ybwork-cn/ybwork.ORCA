::设置分支名字
SET ToolName=upm
::设置模块源路径
SET ToolAssetPath=Assets/ybwork.ORCA

git branch -D %ToolName%
git remote rm %ToolName%
::此命令会创建一个ToolName的分支，并同步ToolAssetPath下的内容
git subtree split -P %ToolAssetPath% --branch %ToolName%

pause
