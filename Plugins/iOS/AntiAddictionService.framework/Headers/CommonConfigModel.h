//
//  CommonConfigModel.h
//  AntiAddictionService
//
//  Created by jessy on 2021/9/22.
//  Copyright © 2021 JiangJiahao. All rights reserved.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface CommonConfigModel : NSObject

+ (instancetype)sharedInstance;

@property (nonatomic,strong) NSArray *auth_identify_words;

// 实名相关配置

@property (nonatomic,strong) NSDictionary * real_name_text;

@property (nonatomic,assign) BOOL manual_auth_enable;

//政策及对应提示文案
@property (nonatomic,strong) NSDictionary * anti_addiciton_strategy;

//本地模式下文案及时间段
@property (nonatomic,strong) NSDictionary * time_range;



@end

NS_ASSUME_NONNULL_END
