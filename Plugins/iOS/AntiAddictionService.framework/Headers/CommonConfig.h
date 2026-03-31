//
//  CommonConfig.h
//  AntiAddictionService
//
//  Created by jessy on 2021/9/22.
//  Copyright © 2021 JiangJiahao. All rights reserved.
//

#import <Foundation/Foundation.h>
//#import <AntiAddictionService/AntiAddictionService-Swift.h>

NS_ASSUME_NONNULL_BEGIN

@interface CommonConfig : NSObject



+ (void)saveRealNameConfigToJsonFile:(NSDictionary *)dic;
+ (nullable NSDictionary *)loadRealNameConfigFromJsonFile;


+ (void)saveUserConfigToJsonFile:(NSString *) userId data:(NSDictionary *)dic;
+ (nullable NSDictionary<NSString* ,NSObject*> *)loadUserConfigFromJsonFile:(NSString *) userId;

+(void)clearUserConfig:(NSString *) userId;

@end

NS_ASSUME_NONNULL_END
