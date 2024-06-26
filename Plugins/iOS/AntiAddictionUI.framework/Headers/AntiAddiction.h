//
//  AntiAddiction.h
//  AntiAddictionUI
//
//  Created by jessy on 2021/9/22.
//

#import <Foundation/Foundation.h>
#import <AntiAddictionService/AntiAddictionService-Swift.h>
#import <AntiAddictionUI/AntiAddictionUI.h>

#define AntiAddictionSDK @"AntiAddiction"


NS_ASSUME_NONNULL_BEGIN

typedef NS_ENUM(NSInteger, AntiAddictionResultHandlerCode) {
    AntiAddictionResultHandlerLoginSuccess          = 500,   //登录成功
    AntiAddictionResultHandlerExited                = 1000,   //登出成功
    AntiAddictionResultHandlerSwitchAccount         = 1001,  // 切换账号
    AntiAddictionResultHandlerPeriodRestrict        = 1030,  // 不可玩时间段游戏
    AntiAddictionResultHandlerAgeLimit              = 1100,  // 适龄限制
    AntiAddictionResultHandlerInvalidClientOrNetworkError        = 1200,  // 无效应用 ID 或网络异常

    AntiAddictionResultHandlerRealNameStop          = 9002,  // 实名过程中点击了关闭实名窗
    
    AntiAddictionResultHandlerLoginLogout __attribute__((deprecated("Please use AntiAddictionResultHandlerExited"))) = 1000,  //用户登出
    AntiAddictionResultHandlerTimeLimit __attribute__((deprecated("Please use AntiAddictionResultHandlerPeriodRestrict"))) = 1030,  // 用户当前无法进行游戏
    AntiAddictionResultHandlerOpenAlert __attribute__((deprecated("Not supported"))) = 1095,//未成年允许游戏弹窗

};

typedef NS_ENUM(NSInteger, AntiAddictionAgeLimit) {
    AntiAddictionAgeLimitUnknown            = -1,
    AntiAddictionAgeLimitChild              = 0,
    AntiAddictionAgeLimitTeen               = 8,
    AntiAddictionAgeLimitYoung              = 16,
    AntiAddictionAgeLimitAdult              = 18,
};

@protocol AntiAddictionDelegate <NSObject>

- (void)antiAddictionCallbackWithCode:(AntiAddictionResultHandlerCode)code extra:(NSString * _Nullable)extra;

@end


@interface AntiAddiction : NSObject

/// 初始化配置
/// @param config 防沉迷配置
+ (void)initWithConfig:(AntiAddictionConfig *)config;

/// 设置回调代理
/// @param delegate 防沉迷代理
+ (void)setDelegate:(id<AntiAddictionDelegate>)delegate;

/// 设置测试环境，需要在 startup 接口调用前设置
/// @param enable 测试环境是否可用
+ (void)setTestEnvironment:(BOOL)enable;

/// 启动防沉迷&实名系统
/// @param userID 游戏维度的用户唯一标识
+ (void)startupWithUserID:(NSString *)userID __attribute__((deprecated("Please use [AntiAddiction startupWithTapTap:]")));

/// 启动防沉迷&实名系统
/// @param userID 游戏维度的用户唯一标识
+ (void)startupWithUserID:(NSString *)userID isTapUser:(bool)isTapUser __attribute__((deprecated("Please use [AntiAddiction startupWithTapTap:]")));

/// 如果使用的TapTap登录，可以快速启动防沉迷&实名系统
/// @param userID 游戏维度的用户唯一标识
+ (void)startupWithTapTap:(NSString *)userID;

/// 退出防沉迷&实名系统
+ (void)exit;

/// 进入游戏
+ (void)enterGame __attribute__((deprecated("Not supported in future versions")));

/// 离开游戏
+ (void)leaveGame __attribute__((deprecated("Not supported in future versions")));

/// 获取用户剩余时长（单位：秒）
+ (NSInteger)getRemainingTime;

/// 获取用户剩余时长（单位：分钟）
+ (NSInteger)getRemainingTimeInMinutes;

/// 获取用户年龄段
+ (AntiAddictionAgeLimit)getAgeRange;


/// 查询能否支付
/// @param amount 支付金额，单位分
/// @param resultBlock 能否成功
/// @param failureHandler 查询能否支付失败（一般网络错误）
+ (void)checkPayLimit:(NSInteger)amount resultBlock:(void(^ _Nullable)(BOOL status))resultBlock failureHandler:(void (^ _Nullable)(NSString * _Nonnull error))failureHandler;

/// 上报消费结果
/// @param amount 支付金额，单位分
/// @param callBack 上报是否成功
/// @param failureHandler 上报消费结果失败（一般网络错误）
+ (void)submitPayResult:(NSInteger)amount callBack:(void(^ _Nullable)(BOOL success))callBack failureHandler:(void (^ _Nullable)(NSString * _Nonnull error))failureHandler;

///获取当前防沉迷Token
+ (NSString *)currentToken;


+ (void)initWithConfig:(AntiAddictionConfig *)config delegate:(id<AntiAddictionDelegate>)delegate __attribute__((deprecated("Please use [AntiAddiction initWithConfig:] and [AntiAddiction setDelegate:]")));
+ (void)initGameIdentifier:(NSString *)gameIdentifier antiAddictionConfig:(AntiAddictionConfiguration *)config  antiAddictionCallbackDelegate:(id<AntiAddictionDelegate>)delegate completionHandler:(void(^)(BOOL))com __attribute__((deprecated("Please use [AntiAddiction initWithConfig:delegate:]")));
+ (void)startUpUseTapLogin:(BOOL)useTapLogin userIdentifier:(NSString *)userIdentifier __attribute__((deprecated("Please use [AntiAddiction startupWithTapTap:]")));
+ (NSInteger)getCurrentUserAgeLimite __attribute__((deprecated("Please use [AntiAddiction getAgeRange]")));
+ (NSInteger)getCurrentUserRemainTime __attribute__((deprecated("Please use [AntiAddiction getRemainingTime]")));
+ (void)checkPayLimit:(NSInteger)amount callBack:(void (^ _Nullable)(BOOL status, NSString * _Nonnull title, NSString *  _Nonnull description))callBack failureHandler:(void (^ _Nullable)(NSString * _Nonnull))failureHandler __attribute__((deprecated("Please use [AntiAddiction checkPayLimit:resultBlock:failureHandler:]")));
+ (BOOL)isStandAlone __attribute__((deprecated("Not supported in future versions")));
+ (void)logout __attribute__((deprecated("Please use [AntiAddiction exit]")));
+ (NSString *)getSDKVersion __attribute__((deprecated("Please use AntiAddictionSDK_VERSION")));

///   获取实名信息
/// @param gameIdentifier  游戏唯一标识
/// @param userIdentifier  游戏维度的用户唯一标识
/// @param successHandelr AntiAddictionRealNameAuthState 用户状态，antiAddictionToken 防沉迷Token，AntiAddictionRealNameAgeLimit 年龄区分
/// @param failureHandler msg 错误信息
+ (void)fetchUserIndentifyInfoGameIdentifier:(NSString *)gameIdentifier userIdentifier:(NSString *)userIdentifier
                              successHandler:(void(^)(AntiAddictionRealNameAuthState state, NSString *antiAddictionToken, AntiAddictionRealNameAgeLimit ageLimit))successHandelr
                              failureHandler:(void(^)(NSString *msg))failureHandler __attribute__((deprecated("Not supported in future versions")));


+ (void)setUnityVersion:(NSString *)version;

@end

NS_ASSUME_NONNULL_END
