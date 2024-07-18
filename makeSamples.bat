
SET ToolAssetPath=Assets\ybwork.ORCA

::删除Samples~文件夹
rd /s /q %ToolAssetPath%\Samples~
::复制删除Samples文件夹到Samples~文件夹
xcopy /e /i /y Assets\Samples\ %ToolAssetPath%\Samples~

pause
