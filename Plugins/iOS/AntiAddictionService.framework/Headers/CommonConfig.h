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

+ (void)dataFromJson;
+ (void)dataFromJsonWithConfig:(NSDictionary *)data_dict;

@end

NS_ASSUME_NONNULL_END
