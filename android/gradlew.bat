@ECHO OFF
SETLOCAL
SET DIRNAME=%~dp0
IF "%JAVA_HOME%"=="" SET JAVA_HOME=C:\Program Files\Microsoft\jdk-17.0.20.8-hotspot
SET CLASSPATH=%DIRNAME%gradle\wrapper\gradle-wrapper.jar
"%JAVA_HOME%\bin\java.exe" -Xmx64m -Xms64m -classpath "%CLASSPATH%" org.gradle.wrapper.GradleWrapperMain %*
ENDLOCAL
