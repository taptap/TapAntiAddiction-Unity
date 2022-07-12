//
//  AntiAddiction.h
//  AntiAddictionUI
//
//  Created by jessy on 2021/9/22.
//

#import <Foundation/Foundation.h>
#import <AntiAddictionService/AntiAddictionService-Swift.h>
#import <AntiAddictionService/AntiAddictionHttpManager.h>

#define AntiAddictionSDK @"AntiAddiction"
#define AntiAddictionSDK_VERSION_NUMBER @"31100001"
#define AntiAddictionSDK_VERSION        @"3.11.0"

NS_ASSUME_NONNULL_BEGIN


typedef NS_ENUM(NSInteger,AntiAddictionResultHandlerCode) {
    AntiAddictionResultHandlerLoginSuccess  = 500,   //登录成功
    AntiAddictionResultHandlerLoginLogout   = 1000,  //用户登出
    AntiAddictionResultHandlerSwitchAccount = 1001,  // 切换账号
    AntiAddictionResultHandlerTimeLimit     = 1030,  // 用户当前无法进行游戏
    AntiAddictionResultHandlerOpenAlert     = 1095,  // 未成年允许游戏弹窗
    AntiAddictionResultHandlerRealNameStop  = 9002   // 实名过程中点击了关闭实名窗
};

@protocol AntiAddictionDelegate <NSObject>

- (void)antiAddictionCallbackWithCode:(AntiAddictionResultHandlerCode)code extra:(NSString * _Nullable)extra;

@end


@interface AntiAddiction : NSObject


@property(nonatomic,weak)id<AntiAddictionDelegate> antiAddictionCallbackDelegate;

///  初始化
/// @param gameIdentifier  游戏唯一标志符
/// @param config  防沉迷配置
/// @param com  获取配置成功与否
+ (void)initGameIdentifier:(NSString *)gameIdentifier antiAddictionConfig:(AntiAddictionConfiguration *)config  antiAddictionCallbackDelegate:(id<AntiAddictionDelegate>)delegate completionHandler:(void(^)(BOOL))com;

///  启动防沉迷&实名系统
/// @param useTapLogin  是否使用Tap登录
/// @param userIdentifier  游戏维度的用户唯一标识
/// @param tapAccesssToken  Tap中TTSDKAccessToken 的json字符串。当useTapLogin = false 时，useTapLogin传nil 或 空字符串
+ (void)startUpUseTapLogin:(BOOL)useTapLogin userIdentifier:(NSString *)userIdentifier;

///   获取实名信息
/// @param gameIdentifier  游戏唯一标识
/// @param userIdentifier  游戏维度的用户唯一标识
/// @param successHandelr AntiAddictionRealNameAuthState 用户状态，antiAddictionToken 防沉迷Token，AntiAddictionRealNameAgeLimit 年龄区分
/// @param failureHandler msg 错误信息
+ (void)fetchUserIndentifyInfoGameIdentifier:(NSString *)gameIdentifier userIdentifier:(NSString *)userIdentifier
                              successHandler:(void(^)(AntiAddictionRealNameAuthState state, NSString *antiAddictionToken, AntiAddictionRealNameAgeLimit ageLimit))successHandelr
                              failureHandler:(void(^)(NSString *msg))failureHandler;

///   进入游戏
+ (void)enterGame;

/// 离开游戏
+ (void)leaveGame;

/// 获取用户类型
/// - Parameter userId: 用户 id
+ (NSInteger)getCurrentUserAgeLimite;

/// 获取用户剩余时长
+ (NSInteger)getCurrentUserRemainTime;

/// 查询能否支付
/// - Parameter amount: 支付金额，单位分
/// - Parameter status: true:可以付费 false:限制消费
/// - Parameter title: 限制消费时提示标题
/// - Parameter description: 限制消费提示国家法规内容
+ (void)checkPayLimit:(NSInteger)amount callBack:(void (^ _Nullable)(BOOL status, NSString * _Nonnull title, NSString *  _Nonnull description))callBack failureHandler:(void (^ _Nullable)(NSString * _Nonnull))failureHandler;

/// 上报消费结果 
/// - Parameter amount: 支付金额，单位分
+ (void)submitPayResult:(NSInteger)amount callBack:(void(^)(BOOL success))callBack failureHandler:(void (^ _Nullable)(NSString * _Nonnull))failureHandler;

/// 是否是单机模式
+ (BOOL)isStandAlone;

///获取当前防沉迷Token
+ (NSString *)currentToken;

+ (void)logout;

// 获取版本号
+(NSString *)getSDKVersion;

+ (void)setUnityVersion:(NSString *)version;

@end

NS_ASSUME_NONNULL_END
