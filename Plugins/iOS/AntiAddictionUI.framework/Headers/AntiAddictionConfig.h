//
//  AntiAddictionConfig.h
//  AntiAddictionUI
//
//  Created by 黄驿峰 on 2022/8/31.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

typedef NS_ENUM(NSInteger, AntiAddictionRegion) {
    AntiAddictionRegionChina
};

@interface AntiAddictionConfig : NSObject

@property (nonatomic, strong) NSString *clientID;
@property (nonatomic, assign) BOOL useTapLogin __attribute__((deprecated("useTapLogin is not valid any more")));
@property (nonatomic, assign) BOOL showSwitchAccount;
@property (nonatomic, assign) AntiAddictionRegion region;
@property (nonatomic, assign) BOOL useAgeRange;

- (instancetype)init;

@end

NS_ASSUME_NONNULL_END
